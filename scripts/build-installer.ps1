param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version '$Version' is not a valid X.Y.Z semantic version."
}

$RepoRoot = Split-Path -Parent $PSScriptRoot
$IssPath = Join-Path $RepoRoot "installer\SonarSlideVB.iss"
$PublishExe = Join-Path $RepoRoot "artifacts\publish\SonarSlideVB.exe"
$OutputExe = Join-Path $RepoRoot "artifacts\SonarSlideVB-v$Version-win-x64-installer.exe"

if (-not (Test-Path $PublishExe)) {
    throw "Published app not found: $PublishExe. Run scripts\build.ps1 first."
}

$IsccPath = $null
$IsccCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($null -ne $IsccCommand) {
    $IsccPath = $IsccCommand.Source
} else {
    $DefaultIscc = Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"
    if (Test-Path $DefaultIscc) {
        $IsccPath = $DefaultIscc
    }
}

if ($null -eq $IsccPath) {
    throw "Inno Setup compiler (ISCC.exe) was not found. Install Inno Setup 6."
}

& $IsccPath $IssPath "/DAppVersion=$Version"
if ($LASTEXITCODE -ne 0) {
    throw "ISCC.exe failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $OutputExe)) {
    throw "Expected installer output not found: $OutputExe"
}

Write-Host "Installer built at $OutputExe"
