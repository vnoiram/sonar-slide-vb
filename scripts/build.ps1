Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $RepoRoot "SonarSlideVB.csproj"
$PublishDir = Join-Path $RepoRoot "artifacts\publish"
$VsWherePath = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

if (-not (Test-Path $ProjectPath)) {
    throw "Project file not found: $ProjectPath"
}

$MsBuildPath = $null
if (Test-Path $VsWherePath) {
    $VsInstallPath = & $VsWherePath -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($VsInstallPath)) {
        $Candidate = Join-Path $VsInstallPath "MSBuild\Current\Bin\amd64\MSBuild.exe"
        if (Test-Path $Candidate) {
            $MsBuildPath = $Candidate
        }
    }
}

if ($null -eq $MsBuildPath) {
    $MsBuildCommand = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($null -ne $MsBuildCommand) {
        $MsBuildPath = $MsBuildCommand.Source
    }
}

if ($null -eq $MsBuildPath) {
    throw "MSBuild was not found. Install Visual Studio Build Tools with .NET Framework targeting pack."
}

& $MsBuildPath $ProjectPath /t:Build /p:Configuration=Release /p:Platform=x64 /nologo
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE"
}

New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null
Copy-Item -Force -Path (Join-Path $RepoRoot "bin\x64\Release\SonarSlideVB.exe") -Destination $PublishDir
Copy-Item -Force -Path (Join-Path $RepoRoot "bin\x64\Release\SonarSlideVB.exe.config") -Destination $PublishDir -ErrorAction SilentlyContinue
Copy-Item -Force -Path (Join-Path $RepoRoot "bin\x64\Release\SonarSlideVB.pdb") -Destination $PublishDir -ErrorAction SilentlyContinue

Write-Host "Published to $PublishDir"
