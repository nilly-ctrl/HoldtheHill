<#
.SYNOPSIS
  Builds and packages a Hold the Hill graybox release through the Unity session lock.

.DESCRIPTION
  Runs GrayboxReleasePackager.Build in a batch-mode Unity via
  B:/Projects/GameDev/Tools/unity-session-lock.sh (AGENTS.local.md requires every automated
  Unity run to go through it). The zip, changelog and VERSION.txt land in
  <repo>/Builds~/HoldTheHill-Graybox-<version>/ and the same name + .zip.

  Unity allows one instance per project: close the Editor on this project first, and do not run
  this while another AI session holds the lock (the script then exits with code 1).

.PARAMETER SessionId
  Short id that identifies this run in the lock file. Defaults to a random 8-character id.

.PARAMETER Purpose
  Text stored in the lock file.

.PARAMETER ReleaseDir
  Optional parent folder for the output, instead of <repo>/Builds~.

.PARAMETER TimeoutSeconds
  Time limit passed to the lock script (UNITY_AI_TIMEOUT). A cold player build can take a while.

.EXAMPLE
  powershell -File build-release.ps1
#>
[CmdletBinding()]
param(
    [string]$SessionId = ([guid]::NewGuid().ToString('N').Substring(0, 8)),
    [string]$Purpose = 'Release package build',
    [string]$ReleaseDir = '',
    [int]$TimeoutSeconds = 1800
)

$ErrorActionPreference = 'Stop'

$repo = 'B:/Projects/GameDev/Games/hold-the-hill'
$project = "$repo/unity"
$lock = 'B:/Projects/GameDev/Tools/unity-session-lock.sh'
$method = 'HoldTheHill.Sandbox.NillyCtrl.GrayboxReleasePackager.Build'

$bash = (Get-Command bash -ErrorAction SilentlyContinue).Source
if (-not $bash -or $bash -like '*\Windows\System32\*') {
    $bash = 'C:\Program Files\Git\bin\bash.exe'   # the WSL launcher is not what the lock script wants
}
if (-not (Test-Path $bash)) { throw "Git Bash not found at $bash" }
if (-not (Test-Path $lock)) { throw "Lock script not found: $lock" }

$buildsDir = if ($ReleaseDir) { $ReleaseDir } else { "$repo/Builds~" }
New-Item -ItemType Directory -Force -Path $buildsDir | Out-Null
$log = "$buildsDir/build-release-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

Write-Host "Lock status:"
& $bash $lock $project status
if ($LASTEXITCODE -eq 1) {
    Write-Host "Another session holds the Unity lock. Wait for it; do not kill its Unity." -ForegroundColor Yellow
    exit 1
}

$unityArgs = @('-batchmode', '-nographics', '-quit', '-executeMethod', $method, '-logFile', $log)
if ($ReleaseDir) { $unityArgs += @('-hthReleaseDir', $ReleaseDir) }

$env:UNITY_AI_TIMEOUT = "$TimeoutSeconds"
Write-Host "Starting build, session $SessionId. Log: $log"
& $bash $lock $project run $SessionId $Purpose '--' @unityArgs
$code = $LASTEXITCODE

if ($code -eq 0) {
    Write-Host "Done. Output in $buildsDir" -ForegroundColor Green
} else {
    Write-Host "Unity exited with code $code. See $log" -ForegroundColor Red
}
exit $code
