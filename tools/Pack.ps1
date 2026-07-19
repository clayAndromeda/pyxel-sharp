# Builds the native binding and packs PyxelSharp + PyxelSharp.Templates
# into a local NuGet feed (default: %USERPROFILE%\.nuget-local).
#
# NOTE: On machines with Smart App Control, run this script from a detached
# pwsh process (Start-Process pwsh -File tools\Pack.ps1), not from a sandbox.
#
#   tools\Pack.ps1 [-Feed <dir>] [-Version <x.y.z>] [-SkipRustBuild]

param(
    [string]$Feed = (Join-Path $env:USERPROFILE ".nuget-local"),
    [string]$Version = "",
    [switch]$SkipRustBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path $Feed)) {
    New-Item -ItemType Directory -Force $Feed | Out-Null
}

if (-not $SkipRustBuild) {
    if (-not $env:LIBCLANG_PATH) { $env:LIBCLANG_PATH = "C:\Program Files\LLVM\lib" }
    Push-Location (Join-Path $repoRoot "rust\pyxel-bind-cs")
    try {
        cargo build --release --features sdl2_static
        if ($LASTEXITCODE -ne 0) { throw "cargo build failed ($LASTEXITCODE)" }
    }
    finally { Pop-Location }
}

$versionArgs = @()
if ($Version) { $versionArgs = @("-p:Version=$Version") }

dotnet pack (Join-Path $repoRoot "csharp\src\PyxelSharp\PyxelSharp.csproj") `
    -c Release -o $Feed -p:SkipRustBuild=true @versionArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet pack PyxelSharp failed ($LASTEXITCODE)" }

dotnet pack (Join-Path $repoRoot "csharp\templates\PyxelSharp.Templates.csproj") `
    -c Release -o $Feed @versionArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet pack PyxelSharp.Templates failed ($LASTEXITCODE)" }

dotnet pack (Join-Path $repoRoot "csharp\src\PyxelSharp.Editor\PyxelSharp.Editor.csproj") `
    -c Release -o $Feed -p:SkipRustBuild=true @versionArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet pack PyxelSharp.Editor failed ($LASTEXITCODE)" }

Write-Host "Packed to $Feed"
Get-ChildItem $Feed -Filter *.nupkg | ForEach-Object { Write-Host "  $($_.Name)" }
