param(
    [string]$Configuration = "Release",
    [string]$Version = "dev"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\Interlude\Interlude.csproj"
$publishRoot = Join-Path $repoRoot "publish"
$publishDir = Join-Path $publishRoot "win-x64-single-file"
$distDir = Join-Path $repoRoot "dist"
$appExe = Join-Path $distDir "Interlude.exe"
$versionedAppExe = Join-Path $distDir "Interlude-$Version.exe"

function Reset-Directory {
    param([string]$Path)

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path | Out-Null
}

function Assert-SingleFilePublish {
    param([string]$Path)

    $exePath = Join-Path $Path "Interlude.exe"
    $runtimeConfigPath = Join-Path $Path "Interlude.runtimeconfig.json"
    $dllPath = Join-Path $Path "Interlude.dll"

    if (-not (Test-Path -LiteralPath $exePath)) {
        throw "Publish output is missing Interlude.exe."
    }

    if (Test-Path -LiteralPath $runtimeConfigPath) {
        throw "Publish output still has Interlude.runtimeconfig.json. The release must be one executable."
    }

    if (Test-Path -LiteralPath $dllPath) {
        throw "Publish output still has Interlude.dll. The release must be one executable."
    }
}

Reset-Directory -Path $publishDir
Reset-Directory -Path $distDir

Write-Host "Publishing Interlude as one offline Windows x64 executable..."
$publishArgs = @(
    "publish",
    $projectPath,
    "-c", $Configuration,
    "-r", "win-x64",
    "--self-contained", "true",
    "-o", $publishDir,
    "/p:PublishSingleFile=true",
    "/p:EnableCompressionInSingleFile=true",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:DebugType=none",
    "/p:DebugSymbols=false",
    "/p:PublishTrimmed=false",
    "/p:SatelliteResourceLanguages=zh-Hans%3Ben"
)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Assert-SingleFilePublish -Path $publishDir
Copy-Item -LiteralPath (Join-Path $publishDir "Interlude.exe") -Destination $appExe -Force

if ($Version -ne "dev") {
    Copy-Item -LiteralPath $appExe -Destination $versionedAppExe -Force
}

Write-Host ""
Write-Host "Release artifact:"
Write-Host "  $appExe"
if ($Version -ne "dev") {
    Write-Host "  $versionedAppExe"
}
