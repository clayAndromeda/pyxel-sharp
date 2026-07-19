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
| `csharp/samples/` | サンプル (`BouncingBall`, `HelloPyxel`, `JumpGame`, `HeadlessSmoke`, `EditorSmoke`) |
| `csharp/templates/` | `dotnet new pyxel` テンプレートパッケージ |
| `tools/Generate-KeyEnum.ps1` | `key.rs` → `Key.g.cs` 生成スクリプト |
| `tools/Pack.ps1` | NuGet パッケージをローカルフィードへ出力 |

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
