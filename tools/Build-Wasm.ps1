# Builds a PyxelSharp web sample for browser-wasm:
#   emsdk env -> SDL2 port -> emcc shim -> Rust staticlib -> dotnet publish
#
#   tools\Build-Wasm.ps1 [-Project <dir>] [-EmsdkRoot <dir>] [-RustToolchain <name>]
#
# Prerequisites (see docs/WEB_DESIGN.md):
#   - dotnet workload wasm-tools (bundles emscripten 3.1.56)
#   - emsdk installed+activated at the SAME version as the workload
#   - rustup: wasm32-unknown-emscripten target + rust-src on the nightly toolchain
#
# NOTE: On machines with Smart App Control, run from a detached pwsh process.

param(
    [string]$Project = (Join-Path (Split-Path -Parent $PSScriptRoot) "csharp\samples\BouncingBall.Web"),
    [string]$EmsdkRoot = $env:EMSDK,
    [string]$RustToolchain = "nightly-2026-07-14"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $EmsdkRoot) { $EmsdkRoot = "C:\Users\detec\emsdk" }
if (-not (Test-Path (Join-Path $EmsdkRoot "emsdk_env.ps1"))) {
    throw "emsdk not found at '$EmsdkRoot'. Install emsdk and pass -EmsdkRoot (or set EMSDK)."
}

# 1. Prerequisite checks
if (-not (dotnet workload list | Select-String -Quiet "wasm-tools")) {
    throw "dotnet workload 'wasm-tools' is not installed (dotnet workload install wasm-tools)"
}
if (-not (rustup target list --toolchain $RustToolchain --installed | Select-String -Quiet "wasm32-unknown-emscripten")) {
    throw "rustup target wasm32-unknown-emscripten missing on $RustToolchain (rustup target add --toolchain $RustToolchain wasm32-unknown-emscripten)"
}

# 2. emsdk environment + SDL2 port (cached after the first build)
. (Join-Path $EmsdkRoot "emsdk_env.ps1") | Out-Null
embuilder build sdl2
if ($LASTEXITCODE -ne 0) { throw "embuilder build sdl2 failed ($LASTEXITCODE)" }

# 3. emcc.exe shim (Rust's Command::new("emcc") cannot spawn emcc.bat)
$shimDir = Join-Path $repoRoot "tools\emcc-shim"
dotnet build $shimDir -c Release
if ($LASTEXITCODE -ne 0) { throw "emcc shim build failed ($LASTEXITCODE)" }
$env:PATH = (Join-Path $shimDir "bin\Release\net10.0") + ";" + $env:PATH

# 4. Rust staticlib (panic=abort + build-std: .NET links with -fwasm-exceptions,
#    which rejects the JS-EH objects of the prebuilt std)
Push-Location (Join-Path $repoRoot "rust\pyxel-bind-cs")
try {
    $env:RUSTFLAGS = "-C panic=abort -C target-feature=+simd128"
    cargo "+$RustToolchain" rustc --release --target wasm32-unknown-emscripten `
        --features sdl2_dynamic "-Zbuild-std=std,panic_abort" --crate-type staticlib
    if ($LASTEXITCODE -ne 0) { throw "cargo rustc failed ($LASTEXITCODE)" }
}
finally {
    Pop-Location
    Remove-Item Env:RUSTFLAGS -ErrorAction SilentlyContinue
}

# 5. Stage the archive next to the project (stem must match DllImport name)
Copy-Item (Join-Path $repoRoot "rust\pyxel-bind-cs\target\wasm32-unknown-emscripten\release\libpyxel_bind_cs.a") `
    (Join-Path $Project "pyxel_bind_cs.a") -Force

# 6. Publish (SkipRustBuild: the desktop cargo step is not needed here)
dotnet publish $Project -c Release -p:SkipRustBuild=true -p:EmsdkRoot=$EmsdkRoot
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

$wwwroot = Join-Path $Project "bin\Release\net10.0\publish\wwwroot"
Write-Host ""
Write-Host "Web bundle: $wwwroot"
Write-Host "Serve it with any static HTTP server, e.g.:"
Write-Host "  python -m http.server 8080 --directory `"$wwwroot`""
