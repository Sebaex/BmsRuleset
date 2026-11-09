param(
    [Parameter(Mandatory)][string]$Jdk,
    [Parameter(Mandatory)][string]$EndlessDream,
    [Parameter(Mandatory)][string]$Parser,
    [Parameter(Mandatory)][string]$SupportJar,
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference='Stop'
if ((git -C $EndlessDream rev-parse HEAD) -ne 'd4995153b0a759859ebc8f859dd868abae39db5a') { throw 'Endless Dream version mismatch' }
if ((git -C $Parser rev-parse HEAD) -ne '52c5ddcb6bf96a2688c36fee30586c89bfe49f99') { throw 'Parser version mismatch' }
if ((Get-FileHash -LiteralPath $SupportJar).Hash -ne '38101C00CB6918FEBB9E0671558AD9F11D1DB842A699AF1EE80CA5BA217BB249') { throw 'Auxiliary JAR mismatch' }
if (Test-Path -LiteralPath $Output) { throw 'Use a fresh evidence directory' }
New-Item -ItemType Directory -Path $Output | Out-Null
$play=Join-Path $EndlessDream 'core/src/bms/player/beatoraja/play'
$model=Join-Path $Parser 'src/bms/model'
$manager=[IO.File]::ReadAllText((Join-Path $play 'JudgeManager.java')).Replace("`r`n","`n")
$start=$manager.IndexOf('                    state.lanemodel.reset();',[StringComparison]::Ordinal)
$marker='                    multiBadCollector.filter(tnote);'
$end=$manager.IndexOf($marker,$start,[StringComparison]::Ordinal)+$marker.Length
if ($start -lt 0 -or $end -le $start) { throw 'Selection slice delimiters not found' }
$fragment=$manager.Substring($start,$end-$start)
$fragment | Set-Content (Join-Path $Output 'selection-fragment.txt') -Encoding utf8NoBOM
$wrapper=@'
import bms.model.*;
import bms.player.beatoraja.play.*;
import bms.player.beatoraja.play.JudgeProperty.MissCondition;
import java.lang.reflect.*;
import java.util.*;
public final class JudgeSelectionSlice {
    static final class Lane {
        final Note[] notes; int index;
        Lane(Note[] notes) { this.notes=notes; }
        void reset() { index=0; }
        Note getNote() { return index<notes.length ? notes[index++] : null; }
    }
    static final class State { final Lane lanemodel; State(Note[] notes) { lanemodel=new Lane(notes); } }
    public record Result(int primary,int judge,String additional) {}
    public static Result select(Note[] notes,long pmtime,int rank,boolean scratch,JudgeAlgorithm algorithm,int[] rates) throws Exception {
        State state=new State(notes);
        long[][] mjudge=JudgeProperty.LR2.getJudge(scratch ? JudgeProperty.NoteType.SCRATCH : JudgeProperty.NoteType.NOTE,rank,rates);
        long mjudgestart=0,mjudgeend=0;
        for(long[] row:mjudge) { mjudgestart=Math.min(mjudgestart,row[0]); mjudgeend=Math.max(mjudgeend,row[1]); }
        MissCondition miss=MissCondition.ALWAYS;
        CollectorAdapter multiBadCollector=new CollectorAdapter();
        // ORIGINAL_SELECTION_FRAGMENT
        return new Result(Arrays.asList(notes).indexOf(tnote),tnote==null ? 6 : judge,multiBadCollector.indices(notes));
    }
    static final class CollectorAdapter {
        final Object target; final Class<?> type;
        CollectorAdapter() throws Exception {
            Class<?> unsafe=Class.forName("sun.misc.Unsafe");
            Field singleton=unsafe.getDeclaredField("theUnsafe"); singleton.setAccessible(true);
            Object manager=unsafe.getMethod("allocateInstance",Class.class).invoke(singleton.get(null),JudgeManager.class);
            type=Class.forName("bms.player.beatoraja.play.JudgeManager$MultiBadCollector");
            Constructor<?> constructor=type.getDeclaredConstructor(JudgeManager.class); constructor.setAccessible(true);
            target=constructor.newInstance(manager);
        }
        Object call(String name,Class<?>[] parameters,Object... args) throws Exception {
            Method method=type.getDeclaredMethod(name,parameters); method.setAccessible(true); return method.invoke(target,args);
        }
        void clear() throws Exception { call("clear",new Class<?>[]{}); }
        void setJudge(long[][] table) throws Exception { call("setJudge",new Class<?>[]{long[][].class},(Object)table); }
        void add(Note note,long time) throws Exception { call("add",new Class<?>[]{Note.class,long.class},note,time); }
        void filter(Note note) throws Exception { call("filter",new Class<?>[]{Note.class},note); }
        String indices(Note[] notes) throws Exception {
            Field start=type.getDeclaredField("arrayStart"),size=type.getDeclaredField("size"),list=type.getDeclaredField("noteList");
            start.setAccessible(true); size.setAccessible(true); list.setAccessible(true);
            Note[] retained=(Note[])list.get(target); StringJoiner result=new StringJoiner("|");
            for(int i=start.getInt(target);i<size.getInt(target);i++) result.add(Integer.toString(Arrays.asList(notes).indexOf(retained[i])));
            return result.length()==0 ? "-" : result.toString();
        }
    }
}
'@
$wrapper=$wrapper.Replace('// ORIGINAL_SELECTION_FRAGMENT',$fragment)
$generated=Join-Path $Output 'JudgeSelectionSlice.java'
[IO.File]::WriteAllText([IO.Path]::GetFullPath($generated),$wrapper)
$files=@('JudgeProperty','JudgeAlgorithm','JudgeManager','GaugeProperty','GrooveGauge','BMSPlayerRule') | ForEach-Object { Join-Path $play "$_.java" }
$files+=@('BMSModel','Note','NormalNote','MineNote','LongNote','TimeLine','Mode','BMSModelUtils','BMSDecoder','Section','ChartDecoder','ChartInformation','DecodeLog') | ForEach-Object { Join-Path $model "$_.java" }
$files+=Join-Path $EndlessDream 'core/src/bms/player/beatoraja/ScoreData.java'
$files+=Join-Path $PSScriptRoot 'FixedJudgeOracle.java'
$files+=$generated
& (Join-Path $Jdk 'bin/javac.exe') -encoding UTF-8 -d $Output -classpath $SupportJar -sourcepath $Output @files 2> (Join-Path $Output 'javac.log')
if ($LASTEXITCODE) { throw 'Compilation failed; see javac.log' }
$separator=[IO.Path]::PathSeparator
& (Join-Path $Jdk 'bin/java.exe') -classpath "$Output$separator$SupportJar" FixedJudgeOracle 2> (Join-Path $Output 'java-stderr.log') | Set-Content (Join-Path $Output 'selection.csv') -Encoding utf8NoBOM
if ($LASTEXITCODE) { throw 'Original fragment/collector execution failed' }
$utf8=[Text.UTF8Encoding]::new($false)
$provenance=[ordered]@{
    endless_dream='d4995153b0a759859ebc8f859dd868abae39db5a'; parser='52c5ddcb6bf96a2688c36fee30586c89bfe49f99'
    support_jar_sha256=(Get-FileHash $SupportJar).Hash
    scope='Verbatim JudgeManager.update selection slice plus original JudgeAlgorithm/JudgeProperty/MultiBadCollector; adapted ordered lane enumeration; no player/updateMicro or graphical loop execution'
    slice_start_line=($manager.Substring(0,$start) -split "`n").Count
    slice_end_line=($manager.Substring(0,$end) -split "`n").Count
    slice_sha256_lf_utf8=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($fragment)))
    files=@($files | ForEach-Object { [ordered]@{file=(Split-Path $_ -Leaf);sha256_lf_utf8=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes([IO.File]::ReadAllText($_).Replace("`r`n","`n"))))} })
    rows=@(Import-Csv (Join-Path $Output 'selection.csv')).Count
}
$provenance|ConvertTo-Json -Depth 6|Set-Content (Join-Path $Output 'PROVENANCE.json') -Encoding utf8NoBOM
