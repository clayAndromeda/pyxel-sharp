# Stage 6: Web 対応 設計メモ

2026-07-19 の設計レビュー合意 ([TODO.md](TODO.md) 参照) に基づく詳細設計。
スパイク着手時点の調査結果を含む。

## 1. 全体アーキテクチャ

```
ブラウザ
└─ index.html + main.js (canvas 要素 + dotnet.js 起動グルー)
   └─ dotnet.js (.NET browser-wasm ランタイム、emscripten ビルド)
      ├─ マネージド: ゲーム.dll + PyxelSharp.dll (IL、まずインタプリタ実行)
      └─ dotnet.native.wasm に静的リンクされるネイティブ群:
         ├─ pyxel_bind_cs.a  (Rust: pyxel-bind-cs + pyxel-core, wasm32-unknown-emscripten)
         └─ libSDL2.a        (emscripten の SDL2 ポート)
```

- デスクトップ版: `cdylib` (pyxel_bind_cs.dll) を P/Invoke で動的ロード
- Web 版: 同じクレートを **staticlib** でビルドし、.NET の
  `NativeFileReference` で dotnet.native.wasm に**静的リンク**する。
  DllImport("pyxel_bind_cs") は .NET wasm のビルド時に静的シンボルへ解決される
  (pinvoke テーブル生成。ライブラリ名は .a のファイル名ステムと一致させる必要が
  あるため、`libpyxel_bind_cs.a` → `pyxel_bind_cs.a` にリネームして参照する)
- C# コード (PyxelSharp / ゲーム / エディタ移植) は**無変更**が目標。
  変わるのはビルド構成のみ

## 2. 調査で確定した事実

### pyxel-core は emscripten 対応済み (本家 Web 版と同じコア)

- `crates/pyxel-core/src/platform/` に `target_os = "emscripten"` 分岐があり、
  メインループは `emscripten_set_main_loop_arg(callback, arg, 0, 1)` で
  requestAnimationFrame 駆動になる
- つまり Rust 側の移植作業はほぼ不要。ビルドターゲットの追加だけ

### 本家の WASM ビルドレシピ (pyxel/Makefile より)

```
embuilder build sdl2 --pic          # emscripten の SDL2 ポートをビルド
TARGET=wasm32-unknown-emscripten
RUSTFLAGS:
  -C panic=abort
  -C target-feature=+simd128
  -C link-arg=-fwasm-exceptions
  -C link-arg=-sSIDE_MODULE=2      # ← Pyodide 用。C# 版では使わない
  -C link-arg=-L<emcache>/sysroot/lib/wasm32-emscripten/pic
  -C link-arg=-lSDL2
  -C link-arg=-lhtml5
cargo -Zbuild-std=std,panic_abort   # ← nightly 必要
features: sdl2_dynamic              # (bundled ビルドではなくポートを参照)
```

C# 版との違い:
- **SIDE_MODULE は使わない**。staticlib を吐き、最終リンクは .NET の emcc が行う
- リンクフラグ (`-lSDL2 -lhtml5` 等) は csproj の `EmccExtraLDFlags` 側に移す
- `-Zbuild-std` (nightly) は本家準拠だが、まず stable + プリビルド std で試し、
  リンクエラーが出たら nightly-2026-07-14 + build-std にフォールバック

### 最大の技術リスク 2 点

1. **emscripten バージョン整合**: .NET 10 の wasm-tools ワークロードが同梱する
   emscripten と、SDL2 ポート / Rust オブジェクトをビルドする emsdk の
   バージョンを一致させる必要がある。ワークロードインストール後に
   `C:\Program Files\dotnet\packs\Microsoft.NET.Runtime.Emscripten.*` の
   バージョンを確認し、emsdk 側で同じバージョンを `emsdk install <ver>` する
2. **メインループの unwind**: `emscripten_set_main_loop(..., simulate_infinite_loop=1)`
   のため `Pyxel.Run` は**戻らない**。JS 例外 `unwind` が
   Rust → P/Invoke → マネージド Main のフレームを突き抜けて dotnet.js まで
   到達する。dotnet.js が `unwind` を致命扱いしないか、ランタイムが
   生き続けるか (`noExitRuntime` 相当) がスパイクの最重要検証点。
   以後のフレームは rAF → main_loop_callback (Rust) → UnmanagedCallersOnly
   (C#) で毎フレーム再入する形になり、これは .NET wasm がサポートする経路

### その他の論点

- **UnmanagedCallersOnly コールバック**: .NET wasm はネイティブ→マネージドの
  関数ポインタ呼び出しをサポート (ビルド時にラッパー生成)。既存の
  Run(update, draw) の仕組みはそのまま動く想定
- **スレッド**: wasm は単一スレッド。PyxelSharp の drop キューは
  「pyxel スレッド上なら即時 drop」経路に常に入るため問題なし
- **アセット**: スパイク対象の BouncingBall はアセット不要。
  本実装では .pyxres 等を emscripten の仮想 FS (MEMFS) にプリロードする
  仕組みが必要 (dotnet.js の VFS 機能 or Module.preRun で FS 書き込み)
- **音声**: SDL2 emscripten ポートが WebAudio にマップ。
  ブラウザの自動再生制限 (ユーザー操作まで AudioContext 停止) は既知の挙動で、
  本家も同じ制約。スパイクでは無視してよい

## 3. スパイク計画 (go/no-go)

### 手順

1. **環境構築** (進行中)
   - [x] `rustup target add wasm32-unknown-emscripten` (stable + nightly-2026-07-14)
   - [x] nightly に rust-src 追加 (build-std フォールバック用)
   - [x] emsdk を `C:\Users\detec\emsdk` にクローン済み
   - [ ] `dotnet workload install wasm-tools` — **管理者権限 (UAC) 待ちでブロック中**。
     `C:\Program Files\dotnet` のため昇格必須。管理者ターミナルで手動実行でも可
   - [ ] ワークロードの emscripten バージョン確認 → `emsdk install/activate <同版>`
   - [ ] `embuilder build sdl2` (PIC 不要。静的リンクなので通常ビルドで良い)
2. **Rust staticlib ビルド**
   - [x] `crate-type = ["cdylib", "staticlib"]` に変更済み (デスクトップに影響なし)
   - [ ] `cargo build --release --target wasm32-unknown-emscripten --features sdl2_dynamic`
     (RUSTFLAGS: `-C panic=abort -C target-feature=+simd128`。リンク系フラグは不要 —
     staticlib はリンクしないため)
3. **wasmbrowser プロジェクト** (スパイクは scratchpad、成功後にリポジトリへ)
   - csproj: `Microsoft.NET.Sdk.WebAssembly` / `RuntimeIdentifier=browser-wasm` /
     `OutputType=Exe` / `WasmMainJSPath=main.js` /
     `NativeFileReference=pyxel_bind_cs.a` /
     `EmccExtraLDFlags`: `-sUSE_SDL=2 -lhtml5` (または `-L<emcache> -lSDL2`)
   - index.html: `<canvas id="canvas">` (SDL2 は Module.canvas を参照するため
     main.js の `withModuleConfig` で canvas を渡す)
   - Program.cs: BouncingBall と同一コード
4. **ブラウザ検証**: ローカル HTTP サーバで配信し、描画 (ボールが跳ねる) と
   入力 (Q キー等) を確認

### go/no-go 基準

- **go**: BouncingBall が最新 Chrome/Edge で描画され、体感 30fps で動く
  (入力は動けば加点。unwind の一度きりのエラーログは許容)
- **no-go**: リンク不能 (emscripten バージョン非互換が解決不能)、
  または unwind で .NET ランタイムが停止しフレームが進まない

### no-go 時のフォールバック候補 (優先順)

1. `-Zbuild-std` + 本家と同一 nightly でリンク互換を取り直す
2. dotnet.js の Module 設定 (`noExitRuntime`) / `MainMethodInvoker` 調整で
   unwind を無害化する
3. Rust 側に「1 フレームだけ進める」FFI (`pyxel_step_frame` 相当) を追加し、
   ループを JS/C# 側 (rAF) から駆動して set_main_loop を回避する
   (pyxel-core に step_frame が既にあるが emscripten では未公開のため、
   バインディング側の工夫または本家非改変の範囲で検討)
4. .NET 側を諦め、ゲームロジックだけ C# → 描画コマンドを JS ブリッジ経由で
   本家 wasm に流す設計 (大工事、最終手段)

## 4. 本実装 (go 後) の構成案

```
csharp/src/PyxelSharp.Web/        # Web 用 MSBuild ターゲット/props (NuGet 化検討)
csharp/samples/BouncingBall.Web/  # Pages 公開するデモ
tools/Build-Wasm.ps1              # embuilder + cargo staticlib + publish の一発化
```

- テンプレート: `dotnet new pyxel-web` を PyxelSharp.Templates に追加
- GitHub Pages: `dotnet publish` の `AppBundle` を gh-pages に配置 (Brotli 圧縮)
- README に Web ビルド手順を追記
- 将来課題 (対象外): スマホのタッチ/仮想ゲームパッド、エディタ Web 版、
  AOT ビルドオプション、サイズ最適化 (IL トリミング)

## 5. スパイク結果 (2026-07-20): **GO**

BouncingBall がブラウザ (.NET browser-wasm) で動作。C# Update/Draw が
rAF → Rust main loop → UnmanagedCallersOnly 経由で毎フレーム呼ばれ、
座標が正しく進むことをコンソールログで確認した。

### 確定した構成 (スパイクで検証済み)

- .NET 10 wasm-tools の emscripten は **3.1.56** → emsdk 3.1.56 を
  `C:\Users\detec\emsdk` に install/activate し `embuilder build sdl2`
- Rust: `cargo +nightly-2026-07-14 rustc --release --target wasm32-unknown-emscripten
  --features sdl2_dynamic "-Zbuild-std=std,panic_abort" --crate-type staticlib`
  + `RUSTFLAGS="-C panic=abort -C target-feature=+simd128"`
  (.NET 10 は -fwasm-exceptions リンクのため、プリビルド std の JS-EH と衝突する。
  panic=abort + build-std で EH を排除するのが必須)
- csproj: `NativeFileReference=pyxel_bind_cs.a` (lib 接頭辞を外して DllImport 名と一致
  させる) + `EmccExtraLDFlags` に `-L<emsdkキャッシュ>/wasm32-emscripten -lSDL2 -lhtml5`
  (パスは**フォワードスラッシュ必須**。-sUSE_SDL=2 は dotnet の読み取り専用パック
  キャッシュにポートをビルドしようとして失敗するため直接リンク)
- `WasmRunWasmOpt=false` + `EmccLinkOptimizationFlag=-O0`:
  nightly LLVM が記録する新 feature 名 (bulk-memory-opt 等) を 3.1.56 の
  wasm-opt が知らないため binaryen 工程を回避 (将来 emscripten 更新で解除可)
- Windows では build.rs の `Command::new("emcc")` が emcc.bat を解決できない →
  emcc.bat へ転送する **emcc.exe シム**を PATH 先頭に置く
- **メインループ**: `Pyxel.Run` は Main からではなく **[JSExport] メソッド経由**で
  呼ぶ (dotnet.run() の Main 経路だと unwind が dotnet.js の exit 処理に落ちて
  ループが死ぬ)。JS 側は `dotnet.create()` + `exports.GameEntry.Start()` を
  try/catch し `'unwind'` を握りつぶす。`withModuleConfig({ canvas, noExitRuntime: true })`
- **marshal-ilgen コンポーネントが必須**: 既定の wasm リンクは
  `libmono-component-marshal-ilgen-stub-static.a` (スタブ) を使うため、
  bool 引数の P/Invoke (`Pyxel.Mouse(true)` 等) で mono が assert 死する。
  csproj に `<_MonoComponent Include="marshal-ilgen" />` を追加して本物を
  リンクする (BouncingBall.Web.csproj 参照)
- **ホストページの JS スタブが必須** (本家 pyxel.js が提供しているもの):
  `_readVirtualGamepadBitmask = () => 0`、`_scanCorrection = []`、
  `resetPyxel = () => location.reload()`。
  無いと初回フレームで ReferenceError → ループ停止 (エラーは静かに握られるので注意)

### 残課題 (本実装で対応)

- [x] canvas サイズ問題は解決 (2026-07-20): **emscripten の SDL2 は canvas の
  CSS サイズをフレームバッファサイズとして採用する** (pyxel が計算した
  ウィンドウサイズより優先)。CSS 未指定の canvas は ~1px に潰れて 3x3 表示に
  なる。ホストページで `canvas { width: 640px; height: 480px; }` のように
  明示するのが必須 (本家 pyxel.js も CSS でサイズ制御)。本実装ではレスポンシブ
  CSS (アスペクト比維持) にする
- [x] 可視タブでの目視確認 (ボール描画・体感 fps)。非表示タブでは rAF が止まる
  (検証は setTimeout ポリフィル `?forceRaf=1` で実施)
- [x] キーボード/マウス入力の動作確認
- [x] リポジトリ構成化: csharp/samples/BouncingBall.Web + tools/Build-Wasm.ps1
  (emsdk セットアップ + cargo staticlib + publish の一発化)、スパイクの
  forceRaf/trace ハックの除去
- [x] GitHub Pages デモ + README 手順

## 6. 本実装の確定構成 (2026-07-20、Stage 6 完了)

### 共通ホスト資産 (csharp/src/PyxelSharp.Web/)

Web ゲーム 1 本あたりの固有ファイルを「csproj ~10 行 + index.html + Program.cs
([JSExport] GameEntry.Start)」まで削減した。

- **PyxelSharp.Web.props**: RID (browser-wasm) / OutputType / EmsdkRoot 解決 /
  EmccExtraLDFlags (SDL2 ポート直接リンク) / wasm-opt 回避 (TEMPORARY) /
  marshal-ilgen / PyxelSharp への ProjectReference / staticlib の
  NativeFileReference / 前提チェック Target を集約。サンプルは
  `<Import Project="..\..\src\PyxelSharp.Web\PyxelSharp.Web.props" />` するだけ
- **wwwroot/pyxel-boot.js** (Content として各 wwwroot に注入):
  pyxel-core が要求する JS スタブ (`_readVirtualGamepadBitmask` /
  `_scanCorrection` / `resetPyxel`) + dotnet.js 起動 + unwind 握りつぶし +
  `?forceRaf=1` テスト補助を一本化した `bootPyxel(options)` を export
- **PyxelWebHost.cs** (Compile として各ゲームの主アセンブリに注入):
  `[JSExport] PyxelWebHost.WriteFile(path, bytes)` — JS からの MEMFS 書き込み口
- staticlib は Build-Wasm.ps1 が
  `rust/pyxel-bind-cs/target/wasm32-unknown-emscripten/release/pyxel_bind_cs.a`
  へリネームステージし、props がそこを参照 (プロジェクトごとのコピー廃止)

### アセットロード (.pyxres 等) — JumpGame.Web で検証済み

**実行時 fetch → MEMFS 書き込み**方式。index.html 側:

```js
await bootPyxel({
  clickToStart: true,
  assets: [{ url: './assets/jump_game.pyxres', path: '/assets/jump_game.pyxres' }],
});
```

bootPyxel が fetch → `exports.PyxelWebHost.WriteFile` → `File.WriteAllBytes`。
.NET の System.IO と pyxel-core の std::fs は同一 wasm モジュールの emscripten
MEMFS を共有するため、そのまま `Pyxel.Load("/assets/...")` できる。
アセットファイル自体は csproj の `<Content Link="wwwroot\assets\..." />` で
静的配信に含める。

**採用しなかった案**: ビルド時 VFS (`WasmFilesToIncludeInFileSystem`) は
Microsoft.NET.Sdk.WebAssembly では機能しない。この SDK の boot config は
Microsoft.NET.Sdk.WebAssembly.Tasks が dotnet.js に埋め込む形式で生成され、
vfs エントリを一切出力しない (vfs は runtime pack の WasmAppBuilder +
`WasmGenerateAppBundle=true` 経路専用。dotnet.js ランタイム自体は vfs 対応
コードを持つが、設定を書く側が対応していない)。

### 音声 (自動再生制限) — JumpGame.Web で検証済み

`clickToStart: true` で **Pyxel.Init/Run 自体をクリック後まで遅延**する
(AudioContext がユーザージェスチャ内で生成され、最初から running になる)。
クリック後に AudioContext state=running (48kHz)、SDL2 の ScriptProcessorNode
稼働 = Playm の BGM 再生を確認。音を鳴らさないゲームはゲート不要 (BouncingBall
は即起動)。

### キーボード — JumpGame.Web で検証済み

←/→ 押下保持でプレイヤーが両端まで移動することを確認。`_scanCorrection` スタブは
`[]` のままで安全 (Rust 側 eval が `_scanCorrection[i]||0` のため)。非 US 配列の
文字キー補正が必要になったら本家 pyxel.js の `_CODE_TO_SCANCODE` 実装を
pyxel-boot.js に移植する (将来課題)。

### 計測結果 (2026-07-20、ローカル Chrome)

- **fps**: BouncingBall.Web の FRAME カウンタで 745 フレーム / 約 25 秒 ≈ 30fps
  (pyxel の既定目標値に到達)。インタプリタ実行で十分、AOT 不要
- **サイズ**: publish/wwwroot 非圧縮 ~19MB (dotnet.native.wasm 18MB が支配的)、
  Brotli ~4.6MB (.br/.gz は publish が自動生成)

### ビルドの再現性メモ

- Rust nightly は **Build-Wasm.ps1 の `-RustToolchain` 既定値 (nightly-2026-07-14)
  で固定** (rust-toolchain.toml はデスクトップビルドが stable のため不採用)
- PyxelSharp.csproj は `SkipRustBuild=true` のときデスクトップ dll 不在を許容
  (wasm ビルドは staticlib しか使わないため。クリーンな checkout/worktree からの
  Web ビルドに必要)
- git worktree でビルドする場合: `git submodule update --init` が必要。また
  デスクトップ向け cargo (bundled SDL2 の cmake) は worktree の深いパスで MSVC
  FileTracker が FTK1011 (260 文字制限) で失敗する — デスクトップビルドは
  メインの checkout で行うこと
