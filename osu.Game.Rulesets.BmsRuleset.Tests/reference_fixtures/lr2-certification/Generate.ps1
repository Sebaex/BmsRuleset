param(
    [Parameter(Mandatory)][string]$Jdk,
    [Parameter(Mandatory)][string]$EndlessDream,
    [Parameter(Mandatory)][string]$Parser,
    [Parameter(Mandatory)][string]$SupportJar,
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $Output) { throw 'Use a new output directory to preserve earlier evidence.' }
New-Item -ItemType Directory -Path $Output | Out-Null
$play = Join-Path $EndlessDream 'core/src/bms/player/beatoraja/play'
$model = Join-Path $Parser 'src/bms/model'
$files = @('JudgeProperty','JudgeAlgorithm','JudgeManager','GaugeProperty','GrooveGauge','BMSPlayerRule') | ForEach-Object { Join-Path $play ($_.ToString()+'.java') }
$files += @('BMSModel','Note','NormalNote','LongNote','TimeLine','Mode') | ForEach-Object { Join-Path $model ($_.ToString()+'.java') }
$files += @('WindowOracle','AlgorithmOracle','CollectorOracle') | ForEach-Object { Join-Path $PSScriptRoot ($_.ToString()+'.java') }
& (Join-Path $Jdk 'bin/javac.exe') -encoding UTF-8 -d $Output -classpath $SupportJar -sourcepath $Output @files
if ($LASTEXITCODE) { throw 'Original Java source compilation failed.' }
foreach ($entry in @(@('WindowOracle','windows'), @('AlgorithmOracle','algorithms'), @('CollectorOracle','collector'))) {
    & (Join-Path $Jdk 'bin/java.exe') -classpath "$Output;$SupportJar" $entry[0] | Set-Content -Encoding utf8NoBOM (Join-Path $Output ($entry[1]+'.csv'))
    if ($LASTEXITCODE) { throw ('Oracle failed: '+$entry[0]) }
    if ((Get-FileHash (Join-Path $Output ($entry[1]+'.csv'))).Hash -ne (Get-FileHash (Join-Path $PSScriptRoot ($entry[1]+'.csv'))).Hash) {
        throw ('Fixture differs: '+$entry[1])
    }
}
