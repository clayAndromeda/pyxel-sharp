# Pyxel-Sharp

[Pyxel](https://github.com/kitao/pyxel) のレトロゲームエンジン (Rust 製 `pyxel-core`) を
C# から使えるようにするバインディング。設計の詳細は [docs/DESIGN.md](docs/DESIGN.md) を参照。

```csharp
using PyxelSharp;

float x = 80, dx = 1.5f;

Pyxel.Init(160, 120, title: "Hello Pyxel-Sharp", quitKey: Key.Q);
Pyxel.Run(Update, Draw);

void Update() { x += dx; if (x < 8 || x > Pyxel.Width - 8) dx = -dx; }
void Draw()
{
    Pyxel.Cls(Color.Navy);
    Pyxel.Circ(x, 60, 8, Color.Yellow);
}
```

## 導入 (NuGet ローカルフィード)

ビルド環境 (Rust/LLVM/CMake) なしでゲームを書くには、ローカルフィードの NuGet パッケージを使う。

```powershell
# 1. (初回のみ、このリポジトリで) パッケージをビルドしてフィードに出力
tools\Pack.ps1                       # 既定フィード: %USERPROFILE%\.nuget-local

# 2. (初回のみ) フィードとテンプレートを登録
dotnet nuget add source "$env:USERPROFILE\.nuget-local" --name pyxel-local
dotnet new install PyxelSharp.Templates

# 3. ゲームを作る
dotnet new pyxel -o MyGame
cd MyGame
dotnet run
```

配布用 exe は `dotnet publish -c Release -r win-x64 --self-contained` で作成できる
(`runtimes/win-x64/native/pyxel_bind_cs.dll` が自動同梱される)。

## Web (ブラウザで動かす)

本家 `app2html` 相当。ゲームを .NET browser-wasm + emscripten で静的サイト化できる。
デモ: https://clayandromeda.github.io/pyxel-sharp/

Web ゲームの構成は `csharp/src/PyxelSharp.Web/PyxelSharp.Web.props` を Import した
csproj + `[JSExport] GameEntry.Start` + index.html の 3 点セット
(サンプル: `BouncingBall.Web` = 最小構成、`JumpGame.Web` = .pyxres アセット +
音声 + キーボードあり)。.pyxres 等のアセットは `bootPyxel({ assets: [...] })` が
fetch して MEMFS に書き込み、音を鳴らすゲームは `clickToStart: true` で
ブラウザの自動再生制限を回避する。実測 30fps / 配布サイズ非圧縮 ~19MB
(Brotli ~4.6MB)。ビルド手順は後述の「[開発手順 > Web](#web-browser-wasm)」を参照。

.pyxres リソースの編集にはリソースエディタ (`pyxel edit` 相当の C# 移植) を使う:

```powershell
dotnet tool install --global PyxelSharp.Editor   # 初回のみ (フィード登録済み前提)
pyxel-edit my_resource.pyxres                    # image/tilemap/sound/music の4タブ
```

## 構成

| パス | 内容 |
|---|---|
| `pyxel/` | 本家 Pyxel (git submodule、無改変) |
| `rust/pyxel-bind-cs/` | `extern "C"` バインディングクレート (csbindgen が C# P/Invoke を自動生成) |
| `csharp/src/PyxelSharp/` | 公開 API (`static class Pyxel`, `Key` enum, `Color` struct) + 自動生成 P/Invoke (`NativeMethods.g.cs`) |
| `csharp/src/PyxelSharp.Editor/` | リソースエディタ (本家 Python editor の移植、dotnet tool `pyxel-edit`) |
| `csharp/src/PyxelSharp.Web/` | Web (browser-wasm) 共通ビルド設定 + 起動グルー (props / pyxel-boot.js / PyxelWebHost.cs) |
| `csharp/samples/` | サンプル (`BouncingBall(.Web)`, `HelloPyxel`, `JumpGame(.Web)`, `HeadlessSmoke`, `EditorSmoke`) |
| `csharp/templates/` | `dotnet new pyxel` テンプレートパッケージ |
| `tools/Generate-KeyEnum.ps1` | `key.rs` → `Key.g.cs` 生成スクリプト |
| `tools/Pack.ps1` | NuGet パッケージをローカルフィードへ出力 |
| `tools/Build-Wasm.ps1` | Web サンプルの一括ビルド (emsdk → Rust staticlib → dotnet publish) |

## 開発手順

このリポジトリ自体を開発する (バインディング・エディタ・サンプルに手を入れる) ための手順。
ゲームを書くだけなら前述の NuGet ローカルフィードで足りる。

### Windows (デスクトップ)

必要環境:

| ツール | 用途 |
|---|---|
| Rust (stable) + Cargo | `pyxel_bind_cs.dll` のビルド |
| .NET 10 SDK | C# 側 (`csharp/global.json` でバージョン固定) |
| CMake | bundled SDL2 のビルド (`sdl2_static`) |
| LLVM (libclang) | bindgen 用。`LIBCLANG_PATH` (既定 `C:\Program Files\LLVM\lib`) |

```powershell
git clone --recursive https://github.com/clayAndromeda/pyxel-sharp.git
cd pyxel-sharp
# 既存クローンでサブモジュールが空の場合: git submodule update --init

# Rust バインディング (SDL2 はソースから静的リンクされる)
cargo build --release --features sdl2_static --manifest-path rust\pyxel-bind-cs\Cargo.toml

# C# (Rust ビルドは MSBuild ターゲットから自動実行される。スキップは -p:SkipRustBuild=true)
dotnet build csharp\PyxelSharp.slnx

# サンプル実行
dotnet run --project csharp\samples\BouncingBall
```

変更後の検証はスモークテスト 2 本 (どちらも headless で全項目 pass が期待値):

```powershell
dotnet run --project csharp\samples\HeadlessSmoke   # エンジン API (FFI 境界) の検証
dotnet run --project csharp\samples\EditorSmoke     # エディタの入力注入テスト
```

FFI (`rust/pyxel-bind-cs/src/`) を変更すると build.rs の csbindgen が
`NativeMethods.g.cs` を再生成する。本家 pyxel サブモジュール更新で `key.rs` が
変わった場合は `tools\Generate-KeyEnum.ps1` で `Key.g.cs` を再生成する。
NuGet パッケージ (PyxelSharp / Templates / Editor) の再発行は `tools\Pack.ps1`。

### Web (browser-wasm)

デスクトップ環境に加えて、以下の 3 点を初回のみセットアップする
(バージョン整合が重要。詳細は [docs/WEB_DESIGN.md](docs/WEB_DESIGN.md)):

```powershell
# 1. .NET の wasm ワークロード (emscripten 3.1.56 同梱)
dotnet workload install wasm-tools

# 2. emsdk を「ワークロードと同じバージョン」で install/activate
#    (同梱版は C:\Program Files\dotnet\packs\Microsoft.NET.Runtime.Emscripten.* で確認)
git clone https://github.com/emscripten-core/emsdk $env:USERPROFILE\emsdk
cd $env:USERPROFILE\emsdk
.\emsdk install 3.1.56
.\emsdk activate 3.1.56

# 3. Rust nightly (build-std 用。バージョンは Build-Wasm.ps1 の既定値に固定)
rustup toolchain install nightly-2026-07-14
rustup target add --toolchain nightly-2026-07-14 wasm32-unknown-emscripten
rustup component add --toolchain nightly-2026-07-14 rust-src
```

ビルドは一発スクリプトで行う (SDL2 ポート → emcc シム → Rust staticlib →
`dotnet publish`。個別手順を覚える必要はない):

```powershell
tools\Build-Wasm.ps1                          # 全 Web サンプル
tools\Build-Wasm.ps1 -Project csharp\samples\JumpGame.Web   # 個別
# emsdk の場所は EMSDK 環境変数 (emsdk_env) か -EmsdkRoot で指定

# 出力: csharp\samples\<サンプル>.Web\bin\Release\net10.0\publish\wwwroot
# ローカル確認 (最新 Chrome/Edge):
python -m http.server 8080 --directory csharp\samples\JumpGame.Web\bin\Release\net10.0\publish\wwwroot
```

新しい Web ゲームは `csharp/samples/JumpGame.Web` を雛形にする:

1. csproj: `<Import Project="..\..\src\PyxelSharp.Web\PyxelSharp.Web.props" />` + TFM。
   アセットは `<Content Link="wwwroot\assets\..." />` で静的配信に含める
2. Program.cs: `public static partial class GameEntry` に `[JSExport] Start()` を置き、
   そこから `Pyxel.Init`/`Pyxel.Run` (Run は戻らず JS 側へ unwind する)
3. index.html: `<canvas id="canvas">` (CSS サイズ必須) + `bootPyxel({...})`。
   音を鳴らすなら `clickToStart: true`、アセットは `assets: [{ url, path }]`

配信は `wwwroot` を任意の静的ホスティングへ (GitHub Pages は gh-pages ブランチに
コピーして `.nojekyll` を置く。itch.io へは wwwroot を zip)。

既知の制約: デスクトップブラウザのみ対応 (タッチ/仮想ゲームパッドは未対応)、
キーボードは英字配列相当、`WasmRunWasmOpt=false` + `-O0` リンクは .NET 側の
emscripten が更新されるまでの暫定措置 (props の TEMPORARY コメント参照)。
