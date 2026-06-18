param(
    [switch]$Minimized
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "src\Interlude\Interlude.csproj"

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetPath = if ($dotnetCommand) {
    $dotnetCommand.Path
} else {
    $defaultDotnetPath = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"
    if (Test-Path $defaultDotnetPath) {
        $defaultDotnetPath
    }
}

if (-not $dotnetPath) {
    Write-Host "dotnet was not found. Please install the .NET 8 SDK first:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

$installedSdks = & $dotnetPath --list-sdks
if (-not $installedSdks) {
    Write-Host ".NET runtime is installed, but no .NET SDK was found." -ForegroundColor Yellow
    Write-Host "This source project needs the .NET 8 SDK to run. Please install it first:"
    Write-Host "https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

$arguments = @("run", "--project", $project)
if ($Minimized) {
    $arguments += @("--", "--minimized")
}

& $dotnetPath @arguments
