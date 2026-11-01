param(
 [string]$UnityPath='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.com',
 [switch]$TestOnly
)
$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$logs=Join-Path $project 'Builds\Logs'
New-Item -ItemType Directory -Force $logs | Out-Null
if(-not(Test-Path -LiteralPath $UnityPath)){throw "Unity executable not found: $UnityPath"}
if($TestOnly){
 & $UnityPath -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testResults (Join-Path $logs 'editmode.xml') -logFile (Join-Path $logs 'editmode.log')
}else{
 & $UnityPath -batchmode -nographics -quit -projectPath $project -executeMethod GameExamBuild.Windows -logFile (Join-Path $logs 'build.log')
}
if($LASTEXITCODE -ne 0){throw "Unity failed ($LASTEXITCODE). Inspect $logs. Close this project's editor before batch execution."}
if(-not $TestOnly -and -not(Test-Path (Join-Path $project 'Builds\Windows\GameExam.exe'))){throw 'Unity exited without producing GameExam.exe.'}
