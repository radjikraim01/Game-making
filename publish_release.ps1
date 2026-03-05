param(
    [string]$Runtime = "win-x64",
    [switch]$NoZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectRoot "DungeonEscapeRaylib.csproj"
$publishRoot = Join-Path $projectRoot "bin\Release\net8.0\$Runtime\publish"
$distRoot = Join-Path $projectRoot "dist"
$packageDir = Join-Path $distRoot "DungeonEscape-$Runtime"
$zipPath = Join-Path $distRoot "DungeonEscape-$Runtime.zip"

if (!(Test-Path $projectFile))
{
    throw "Project file not found: $projectFile"
}

# Keep dotnet first-time setup local to the repo environment.
$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".dotnet_cli"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME | Out-Null

Write-Host "Publishing self-contained single-file build for $Runtime ..."
dotnet publish $projectFile `
    -c Release `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true
if ($LASTEXITCODE -ne 0)
{
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

if (!(Test-Path $publishRoot))
{
    throw "Publish output not found: $publishRoot"
}

New-Item -ItemType Directory -Force -Path $distRoot | Out-Null
if (Test-Path $packageDir)
{
    Remove-Item -Path $packageDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null

Write-Host "Copying publish output to dist package folder ..."
Copy-Item -Path (Join-Path $publishRoot "*") -Destination $packageDir -Recurse -Force

if (!$NoZip)
{
    if (Test-Path $zipPath)
    {
        Remove-Item -Path $zipPath -Force
    }

    Write-Host "Creating zip package ..."
    Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath -CompressionLevel Optimal
}

Write-Host ""
Write-Host "Done."
Write-Host "Package folder: $packageDir"
if (!$NoZip)
{
    Write-Host "Zip file: $zipPath"
}
Write-Host "Share the package folder (or zip) with testers."
