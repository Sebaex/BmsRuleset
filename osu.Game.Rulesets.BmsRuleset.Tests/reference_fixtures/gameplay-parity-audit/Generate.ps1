param([Parameter(Mandatory)][string]$Jdk,[Parameter(Mandatory)][string]$EndlessDream,
      [Parameter(Mandatory)][string]$Parser,[Parameter(Mandatory)][string]$SupportJar,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
if((git -C $EndlessDream rev-parse HEAD) -ne 'd4995153b0a759859ebc8f859dd868abae39db5a'){throw 'Wrong Java reference'}
if((git -C $Parser rev-parse HEAD) -ne '52c5ddcb6bf96a2688c36fee30586c89bfe49f99'){throw 'Wrong parser reference'}
if((Get-FileHash $SupportJar).Hash -ne '38101C00CB6918FEBB9E0671558AD9F11D1DB842A699AF1EE80CA5BA217BB249'){throw 'Unknown auxiliary dependency'}
if(Test-Path -LiteralPath $Output){throw 'Use fresh evidence output'}
New-Item -ItemType Directory -Path $Output|Out-Null
$play=Join-Path $EndlessDream 'core/src/bms/player/beatoraja/play'
$model=Join-Path $Parser 'src/bms/model'
$manager=[IO.File]::ReadAllText((Join-Path $play 'JudgeManager.java')).Replace("`r`n","`n")
$start=$manager.IndexOf('        if (judgeVanish) {',$manager.IndexOf('private void updateMicro(LaneState state, Note n, long mtime, int judge, long mfast, boolean judgeVanish, boolean multiBad)'))
$end=$manager.IndexOf('        if (judge != 4)',$start)
if($start -lt 0 -or $end -le $start){throw 'Original score/combo slice not found'}
$slice=$manager.Substring($start,$end-$start)
$caller=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'FlowOracle.java')).Replace("`r`n","`n").Replace('// ORIGINAL_SCORE_COMBO_FRAGMENT',$slice)
$generated=Join-Path $Output 'FlowOracle.java'
[IO.File]::WriteAllText([IO.Path]::GetFullPath($generated),$caller,[Text.UTF8Encoding]::new($false))
$files=@('JudgeProperty','JudgeAlgorithm','JudgeManager','GaugeProperty','GrooveGauge','BMSPlayerRule','LaneProperty')|ForEach-Object {Join-Path $play "$_.java"}
$files+=@('BMSModel','Note','NormalNote','MineNote','LongNote','TimeLine','Mode','BMSModelUtils','BMSDecoder','Section','ChartDecoder','ChartInformation','DecodeLog')|ForEach-Object {Join-Path $model "$_.java"}
$files+=Join-Path $EndlessDream 'core/src/bms/player/beatoraja/ScoreData.java'
$files+=Join-Path $PSScriptRoot 'MineParserOracle.java'
$files+=$generated
& (Join-Path $Jdk 'bin/javac.exe') -encoding UTF-8 -d $Output -classpath $SupportJar -sourcepath $Output @files 2> (Join-Path $Output 'javac.log')
if($LASTEXITCODE){throw 'Original Java compilation failed'}
& (Join-Path $Jdk 'bin/java.exe') -classpath ($Output+[IO.Path]::PathSeparator+$SupportJar) FlowOracle 2> (Join-Path $Output 'java-stderr.log')|Set-Content (Join-Path $Output 'flow.csv') -Encoding utf8NoBOM
if($LASTEXITCODE){throw 'Original flow execution failed'}
& (Join-Path $Jdk 'bin/java.exe') -classpath ($Output+[IO.Path]::PathSeparator+$SupportJar) MineParserOracle 2> (Join-Path $Output 'parser-stderr.log')|Set-Content (Join-Path $Output 'mines.csv') -Encoding utf8NoBOM
if($LASTEXITCODE){throw 'Original mine parse/gauge execution failed'}
$utf8=[Text.UTF8Encoding]::new($false)
[ordered]@{endless_dream='d4995153b0a759859ebc8f859dd868abae39db5a';parser='52c5ddcb6bf96a2688c36fee30586c89bfe49f99';support_jar_sha256=(Get-FileHash $SupportJar).Hash;scope='Verbatim updateMicro score/combo prefix composed with original ScoreData and GrooveGauge; mine gauge branch semantics; no full player, rendering, input polling or GAS lifecycle';slice_start_line=($manager.Substring(0,$start) -split "`n").Count;slice_end_line=($manager.Substring(0,$end) -split "`n").Count;slice_sha256_lf_utf8=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($slice)));files=@($files|ForEach-Object {[ordered]@{file=(Split-Path $_ -Leaf);sha256_lf_utf8=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes([IO.File]::ReadAllText($_).Replace("`r`n","`n"))))}});rows=@(Import-Csv (Join-Path $Output 'flow.csv')).Count}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $Output 'PROVENANCE.json') -Encoding utf8NoBOM
