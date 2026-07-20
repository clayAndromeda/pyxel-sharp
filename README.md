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

```powershell
# 前提: dotnet workload install wasm-tools (emscripten 3.1.56 同梱) と、
# 同じバージョンの emsdk (install/activate 済み)、
# rustup: nightly ツールチェーンに wasm32-unknown-emscripten + rust-src

tools\Build-Wasm.ps1        # SDL2 ポート → Rust staticlib → dotnet publish (全 Web サンプル)
# 出力: csharp\samples\<サンプル>.Web\bin\Release\net10.0\publish\wwwroot
# 任意の静的 HTTP サーバで配信 (itch.io へは wwwroot を zip)
```

Web ゲームの構成は `csharp/src/PyxelSharp.Web/PyxelSharp.Web.props` を Import した
csproj + `[JSExport] GameEntry.Start` + index.html の 3 点セット
(サンプル: `BouncingBall.Web` = 最小構成、`JumpGame.Web` = .pyxres アセット +
音声 + キーボードあり)。.pyxres 等のアセットは `bootPyxel({ assets: [...] })` が
fetch して MEMFS に書き込み、音を鳴らすゲームは `clickToStart: true` で
ブラウザの自動再生制限を回避する。実測 30fps / 配布サイズ非圧縮 ~19MB
(Brotli ~4.6MB)。技術詳細 (ビルドレシピ、emscripten の制約と回避策) は
[docs/WEB_DESIGN.md](docs/WEB_DESIGN.md) を参照。

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

## ビルド

必要環境: Rust (stable), .NET 10 SDK, CMake, LLVM (`LIBCLANG_PATH`, 既定 `C:\Program Files\LLVM\lib`)

```powershell
git clone --recursive https://github.com/clayAndromeda/pyxel-sharp.git
cd pyxel-sharp

# Rust バインディング (SDL2 はソースから静的リンクされる)
cargo build --release --features sdl2_static --manifest-path rust\pyxel-bind-cs\Cargo.toml

# C# (Rust ビルドは MSBuild ターゲットから自動実行される。スキップは -p:SkipRustBuild=true)
dotnet build csharp\PyxelSharp.slnx

# サンプル実行
dotnet run --project csharp\samples\BouncingBall
```
