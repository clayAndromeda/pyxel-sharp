#!/usr/bin/env bash
# Pack.ps1 の macOS/Linux 版。ネイティブバインディングをビルドし、
# PyxelSharp + PyxelSharp.Templates + PyxelSharp.Editor をローカル NuGet
# フィード (既定: ~/.nuget-local) に出力する。
#
#   tools/Pack.sh [--feed <dir>] [--version <x.y.z>] [--skip-rust-build]
#
# PyxelSharp.Web (browser-wasm) は emsdk 前提のため、この .sh では扱わない
# (Web ビルドは Windows の tools/Pack.ps1 / Build-Wasm.ps1 を参照)。
set -euo pipefail

feed="$HOME/.nuget-local"
version=""
skip_rust_build=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --feed)            feed="$2"; shift 2 ;;
        --version)         version="$2"; shift 2 ;;
        --skip-rust-build) skip_rust_build=true; shift ;;
        *) echo "unknown option: $1" >&2; exit 1 ;;
    esac
done

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mkdir -p "$feed"

if [[ "$skip_rust_build" != true ]]; then
    (cd "$repo_root/rust/pyxel-bind-cs" && cargo build --release --features sdl2_static)
fi

version_args=()
if [[ -n "$version" ]]; then
    version_args=("-p:Version=$version")
fi

dotnet pack "$repo_root/csharp/src/PyxelSharp/PyxelSharp.csproj" \
    -c Release -o "$feed" -p:SkipRustBuild=true ${version_args[@]+"${version_args[@]}"}

echo "note: PyxelSharp.Web (browser-wasm) is packed on Windows only; skipping."

dotnet pack "$repo_root/csharp/templates/PyxelSharp.Templates.csproj" \
    -c Release -o "$feed" ${version_args[@]+"${version_args[@]}"}

dotnet pack "$repo_root/csharp/src/PyxelSharp.Editor/PyxelSharp.Editor.csproj" \
    -c Release -o "$feed" -p:SkipRustBuild=true ${version_args[@]+"${version_args[@]}"}

echo "Packed to $feed"
ls -1 "$feed"/*.nupkg | sed 's/^/  /'
