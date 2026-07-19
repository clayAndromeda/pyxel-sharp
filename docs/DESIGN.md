# Pyxel-Sharp 設計ドキュメント

Pyxel のレトロゲームエンジン本体 (Rust 製 `pyxel-core`) を C# から利用できるようにするバインディングプロジェクト。

## 1. Pyxel 本体の構造分析 (Python はどうバインドしているか)

Pyxel リポジトリ (kitao/pyxel) は 3 層構造になっている:

```
crates/pyxel-core     … エンジン本体 (純 Rust、PyO3 に依存しない)
crates/pyxel-binding  … PyO3 製の Python バインディング (cdylib)
python/pyxel          … Python 側の薄いシム (CLI、エディタ等)
```

### pyxel-core (エンジン本体)

- lib 名は `pyxel`。SDL2 (feature: `sdl2_dynamic` / `sdl2_static`) の上に構築。
- **シングルトン設計**: `pyxel::init(...)` で唯一の `Pyxel` インスタンスを生成し、
  `pyxel::pyxel()` が `RefMut<'static, Pyxel>` を返す (thread_local + RefCell、`pyxel.rs:44`)。
  `width()` / `height()` / `frame_count()` / `mouse_x()` などのグローバル状態も同じ
  `define_static!` マクロによる thread_local で公開される。
- **メインループはコールバックトレイト**:
  ```rust
  pub trait PyxelCallback {
      fn update(&mut self);
      fn draw(&mut self);
  }
  Pyxel::run<T: PyxelCallback>(callback: T)  // フレームループを回して update/draw を呼ぶ
  ```
- 型: `Color = u8`, `Key = u32`, `KeyValue = i32`。座標は `f32`。
  Key 定数は SDL キーコード + Pyxel 仮想キー (マウス・ゲームパッドも Key 空間に統合)。
- 破壊的操作の失敗は `Result<(), String>` (例: `init`) か assert (パニック)。

### pyxel-binding (PyO3 層)

- `crate-type = ["cdylib"]`、PyO3 0.29 (abi3-py311)。**pyxel-core に一切手を入れず**、
  外側から thin wrapper を被せているだけ。
- 各モジュール (`system_wrapper.rs`, `graphics_wrapper.rs`, …) が `#[pyfunction]` で
  Python 関数名 (`cls`, `rect`, `btn`, …) → Rust メソッド (`clear`, `draw_rect`,
  `is_button_down`, …) に転送する。
- `run(update, draw)` は Python の callable 2 つを受け取り、`PyxelCallback` を実装した
  アダプタ構造体 (`PythonCallback`) に包んで `Pyxel::run` へ渡す。
- `Image` / `Sound` などの参照型は `Rc<RefCell<T>>` (RcImage 等) を PyO3 クラスで包んで
  ハンドルとして公開する。
- Python 固有の処理 (スクリプトディレクトリへの chdir、reset 時のプロセス再起動、
  atexit 連携) も binding 層に閉じている。

**結論**: エンジンは言語非依存に作られており、「バインディング層だけ差し替える」のが
公式と同じ構図。C# 版は pyxel-binding と対になる `pyxel-bind-cs` クレートを作れば良い。

## 2. Pyxel-Sharp のアーキテクチャ

```
pyxel/  (git submodule、無改変)
  └─ crates/pyxel-core ←──── path 依存
rust/pyxel-bind-cs  (新規 Rust cdylib)  →  pyxel_bind_cs.dll
  │  extern "C" API (pyxel_* 関数群)。PyO3 層の C# 版に相当
  │  build.rs で csbindgen が C# P/Invoke を自動生成
  ↓
csharp/src/PyxelSharp/  (公開 API 層 = 単一アセンブリ)
  ├─ NativeMethods.g.cs  (csbindgen 生成、namespace PyxelSharp.Native、編集禁止)
  └─ Pyxel.*.cs / Key.g.cs / Color.cs  (厳密な型を付与する公開層)
  ↑ 参照
csharp/samples/*  (移植サンプル)
```

### レイヤの責務

| レイヤ | 相当物 | 責務 |
|---|---|---|
| `rust/pyxel-bind-cs` | pyxel-binding | `#[no_mangle] extern "C"` 関数。Option→番兵値、文字列→UTF-8 ポインタ、エラー→戻り値コード+last_error の変換のみ。ロジックは持たない |
| `NativeMethods.g.cs` | (自動生成) | csbindgen が生成した素の P/Invoke。`DllImport` + `delegate* unmanaged[Cdecl]`。namespace は `PyxelSharp.Native` だが PyxelSharp.dll に同居 |
| `PyxelSharp` | python/pyxel | ユーザーが触る唯一の層。`static class Pyxel` + 厳密な型 (`Key` enum, `Color` struct)。例外変換、コールバック橋渡し |

**P/Invoke 層を独立アセンブリにしない理由**: 当初 `PyxelSharp.Native.dll` として分離していたが、
Windows の Smart App Control 有効環境で「DllImport 宣言のみの極小未署名アセンブリ」が
アプリケーション制御ポリシー (0x800711C7) にブロックされる事象を確認。実コードを含む
PyxelSharp.dll に統合すると通るため、単一アセンブリ構成とした (extern シグネチャの
csbindgen 検証は変わらず効く)。

### FFI 規約

- **命名**: ネイティブ関数は `pyxel_` プレフィクス + Python API 名 (`pyxel_cls`, `pyxel_btnp`)。
- **Option の表現**: 数値は番兵値 (`u32::MAX` = None)、文字列は null ポインタ、
  bool フラグは `i32` (-1 = None)。C# 側は `T?` デフォルト引数に変換して隠蔽する。
- **文字列**: UTF-8 `*const c_char`。C# → Rust のみ (MVP 段階)。
- **エラー**: `Result` を返す API は `i32` (0=成功) を返し、メッセージは thread_local に
  保存して `pyxel_last_error()` で取得。C# 層が `PyxelException` に変換して throw。
  (pyxel-core 内部の assert パニックは extern "C" 境界で abort になる。既知の制約)
- **コールバック**: `pyxel_run(update: extern "C" fn(), draw: extern "C" fn())`。
  C# 側は `[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]` の static メソッドを
  関数ポインタで渡し、実体の `Action` は static フィールドに保持 (GC 対策)。
  コールバック内の C# 例外は境界を越えられないため、捕捉して `pyxel_quit()` →
  `Run` リターン後に再 throw する。
- **参照型 (Image, Sound 等)**: 将来ステージで pyxel-binding と同様に
  オペークハンドル (`Box::into_raw` したポインタ) + C# ラッパークラスで公開する。

### C# ならではの厳密な型付け

- `Key` : `uint` ベースの enum。`pyxel-core/src/platform/key.rs` から
  `tools/Generate-KeyEnum.ps1` で自動生成 (`Key.g.cs`)。マウス・ゲームパッドも
  本家同様 Key 空間に統合。
- `Color` : `byte` を包む readonly struct。デフォルトパレット 16 色を
  `Color.Black` 〜 `Color.Peach` として定数公開。`int` からの implicit 変換で
  `Pyxel.Cls(0)` とも書ける。
- 引数は `float` (ネイティブの f32 と一致)。`int` は暗黙変換されるので使用感は Python と同じ。
- `Init` のオプションは C# のオプショナル引数 (`string? title = null, int? fps = null, ...`) で表現。

### ビルドパイプライン

1. `cargo build` (rust/pyxel-bind-cs) — build.rs の csbindgen が
   `NativeMethods.g.cs` を再生成しつつ `pyxel_bind_cs.dll` を出力
2. `dotnet build` — `PyxelSharp.Native.csproj` の MSBuild ターゲットが 1. を自動実行し、
   DLL を出力ディレクトリへコピー (`SkipRustBuild=true` でスキップ可)

要件: Rust (stable)、.NET 10 SDK、CMake、LLVM (`LIBCLANG_PATH`)、
Windows Developer Mode (SDL2 静的ビルド用)。SDL2 は `sdl2_static` feature で
pyxel-core の build.rs がソースからビルド・静的リンクするため、実行時の追加 DLL は不要。

## 3. ステージ計画

| Stage | 内容 | 状態 |
|---|---|---|
| 1 | MVP: system (init/run/quit/flip/show/title/fullscreen) + graphics (cls/pset/pget/line/rect/rectb/circ/circb/elli/ellib/tri/trib/text/clip/camera/dither/pal) + input (btn/btnp/btnr/btnv/mouse) + BouncingBall サンプル | 完了 (2026-07-18) |
| 2 | Image/Tilemap/Font のオペークハンドル、リソースファイル (load/save)、blt/bltm | 完了 (2026-07-18) |
| 3 | audio (Sound/Music/Channel/Tone、play/playm)、math モジュール → Python 版フルパリティ | 完了 (2026-07-18) |
| 4 | 導入体験: GitHub 公開 push、NuGet パッケージ化 (ローカルフィード)、`dotnet new pyxel` テンプレート | 未着手 |
| 5 | リソースエディタ (pyxel edit 相当) の C# 移植 → dotnet tool `pyxel-edit` | 完了 (2026-07-19、目視比較のみ残) |
| 6 | Web 対応: .NET browser-wasm + Rust emscripten 静的リンクで自作ゲームをブラウザ配布 (app2html 相当) | 未着手 (スパイク先行) |

具体的な作業項目は [TODO.md](TODO.md) で管理する。

## 4. 本家に追従する際の注意

- pyxel サブモジュール更新時は `key.rs` の差分を確認し、`Generate-KeyEnum.ps1` を再実行する。
- pyxel-core の公開メソッド名変更は pyxel-bind-cs のコンパイルエラーとして検出される
  (csbindgen 生成コードも同時に更新されるため、シグネチャ不整合が C# 側に漏れない)。
