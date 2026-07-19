# Pyxel-Sharp TODO

ステージ全体像は [DESIGN.md](DESIGN.md) の「ステージ計画」を参照。ここでは具体的な作業項目を管理する。

## Stage 2: グラフィックスリソース (次にやる)

進め方は縦切り: まず Image + blt + サンプル動作確認を完結させ、確立したハンドル
パターンを Tilemap / Font に横展開する (2026-07-18 合意)。

### スライス 1: Image + blt (2026-07-18 完了)

- [x] `Image` のオペークハンドル化 (`Box<RcImage>::into_raw` + C# `Image` クラス)
  - `Pyxel.Images[i]` / `Pyxel.Screen` はキャッシュされアプリ寿命 (Dispose は no-op)
  - `new Image(w, h)` / `Image.FromImage(path)` は IDisposable
- [x] 解放は drop キュー方式: pyxel スレッド上の Dispose は即時 drop、
  別スレッド / ファイナライザは ConcurrentQueue に積み、update コールバック
  先頭と Flip / Run 終了時に消化
- [x] Image インスタンスの描画 API フルセット (Pget/Pset/Load/Save/Set/Data/
  Cls/Line/Rect/Text/Blt/Fill/Clip/Camera/Pal/Dither 等)
- [x] `Pyxel.Blt` + `Pyxel.Fill` (rotate/scale/colkey 含む。int / Image オーバーロード)
- [x] FFI 境界のパニック対策: `ffi!` マクロで全 extern 関数を `catch_unwind` ラップ。
  全関数 i32 戻り値 + 値は out ポインタ + last_error → PyxelException
  (Option<f32> は NaN 番兵、Option<Color> は i32 -1 番兵を追加)
- [x] 検証: HelloPyxel サンプル (01_hello_pyxel 移植、ウィンドウ動作確認済み)
  + HeadlessSmoke に Image / エラー / パニック系 15 項目を追加 (全パス)

### スライス 2: 横展開 (2026-07-18 完了)

- [x] `Tilemap` のオペークハンドル化 + `bltm` (Tile struct + タプル暗黙変換、
  ImageSource は null ハンドル or バンク番号の 2 引数で表現。collide 含む)
- [x] `Font` (`new Font(path, fontSize)`、BDF/TTF + `Pyxel.Text` / `image.Text` の
  font オプション引数 + `TextWidth`)
- [x] `blt3d` / `bltm3d` (スクリーン + Image インスタンス両方)
- [x] リソースファイル: `Load`/`Save` (.pyxres)、`LoadPal`/`SavePal`、
  `Screenshot`/`Screencast`/`ResetScreencast`、`UserDataDir`
- [x] 検証: JumpGame サンプル (02_jump_game 移植、音と rndi は Stage 3 待ちで代替)
  + HeadlessSmoke に tilemap/font/pyxres 系 13 項目追加 (全パス)
- [x] 残りの graphics / system API の棚卸し (2026-07-18 完了):
  `Colors` (表示パレット)、`Cursor`/`FontImage`、`Reset`/`Icon`/`IntegerScale`/
  `ScreenMode`/`Resize` を追加

## Stage 3: audio / math (2026-07-18 完了)

- [x] `Sound` / `Music` / `Channel` / `Tone` のハンドル化
  (Arc<Mutex> なので drop はスレッド不問・即時。配列プロパティはコピー交換)
- [x] `Play` (バンク番号/配列/Sound/Sound[]/MML のオーバーロード) / `Playm` /
  `Stop` / `PlayPos` / `sound.Mml(...)`
- [x] math モジュール (`Rndi`/`Rndf`/`Rseed`/`Nseed`/`Noise`/`Atan2`/`Sin`/`Cos`/
  `Sqrt`/`Ceil`/`Floor`。`clamp`/`sgn` は C# 標準 (`Math.Clamp`/`Math.Sign`) で代替)
- [x] 入力の残り: `InputText` / `InputKeys` / `DroppedFiles` + テスト用 `SetBtn`/
  `SetBtnv`/`SetMousePos`/`SetInputText`/`SetDroppedFiles`
- [x] 検証: JumpGame の音 (`Play`/`Playm`) と `Rndi` を本物に差し替え (動作確認済み)
  + HeadlessSmoke に math/audio/input/colors 系 27 項目追加 (全パス)
- 未対応 (意図的スキップ): `gen_bgm`、deprecated API 群 (old_mml、
  `Tone.noise`/`waveform`、`channel(n)`/`sound(n)`/`music(n)` 等)
- [x] パリティ監査 (__init__.pyi 全 API 突き合わせ) + 定数類の追加 (2026-07-18):
  `Version` (FFI 取得)、サイズ系/バンク数 (`TileSize`/`ImageSize`/`FontWidth` 等は
  C# const、HeadlessSmoke でランタイム値と照合)、`ToneTriangle` 等/`EffectSlide` 等
  (const byte)、`DefaultColors`。Python ランチャ固有定数と CLI/Editor は対象外
- Python サンプル移植による網羅検証は「その他 (時期未定)」へ移動

## Stage 4: 導入体験 (2026-07-18 設計レビューで合意)

ゴール: `dotnet add package PyxelSharp` (ローカルフィード) + `dotnet new pyxel` だけで、
Rust/LLVM/CMake なしにゲームが書ける状態。win-x64 のみ。バージョンは独自 0.x semver
(0.1.0 開始、同梱する本家 pyxel バージョンはパッケージ説明に明記)。TFM は net10.0 のまま
(NuGet.org 公開を決めた時点で net8.0 引き下げを検討)。

- [x] LICENSE 追加 (MIT + 本家 Pyxel への帰属表記) → リポジトリを public で `git push` (2026-07-18)
- [x] NuGet パッケージ化: `runtimes/win-x64/native/pyxel_bind_cs.dll` 同梱、
  pack 時のみ cargo build (`tools/Pack.ps1` 経由、pack は SkipRustBuild=true)。
  消費側には MSBuild ターゲットが伝播しないため Rust ビルドは走らない (2026-07-18)
- [x] ローカルフィード運用: `tools/Pack.ps1` (既定 `%USERPROFILE%\.nuget-local`) +
  README に消費側手順 (nuget add source / new install) を記載 (2026-07-18)
- [x] `dotnet new pyxel` テンプレートパッケージ (`csharp/templates/`、PyxelSharp.Templates):
  最小トップレベル Program.cs 1 枚。専用 CLI ツールは作らない (2026-07-18)
- [x] 検証: 別ディレクトリで new pyxel → フィードから restore → build →
  headless 実行 (pyxel 2.9.8 確認) → ウィンドウ実行 5 秒生存、全パス (2026-07-18)
- 後回し (合意済み): CI (GitHub Actions) は NuGet.org 公開検討時に整備。
  `package`/`app2exe` 相当は `dotnet publish` 手順を README に書くことで代替

## Stage 5: リソースエディタ C# 移植 (2026-07-18 設計レビューで合意)

対象: pyxel/python/pyxel/editor (4,161 行 / 31 ファイル、アセットは editor_220x160.png 1枚)。
構造分析の結論: エンジン API の欠落はゼロ。user_pal/num_user_colors/rect2/get_slice 等は
エディタ自身のモンキーパッチ (→ C# では拡張メソッド + エディタ内状態)、_dropped_files は
wasm レガシー (→ DroppedFiles で代替)。

方針 (合意済み):
- **忠実度**: ファイル構成・クラス分割・レイアウト定数は本家と 1:1 対応を維持
  (本家追従を容易に)。機構だけ C# 化: 動的 var → WidgetVar<T> + 変更通知、
  文字列イベント → C# event、モンキーパッチ → 拡張メソッド。
  配列プロパティ (sound.notes 等) の in-place 変更は read-modify-write に書き換え
- **配置**: csharp/src/PyxelSharp.Editor (exe、ProjectReference)。まずリポ内 exe で
  開発・検証し、完成後に dotnet tool 化 (`pyxel-edit <file>.pyxres`) して Pack.ps1 に組込
- **進め方**: 縦切り 4 スライス。各スライスで動作確認してから次へ
- **検証**: (a) headless 操作テスト (SetBtn/SetMousePos で入力注入 → Pget/状態検証)、
  (b) .pyxres ラウンドトリップ (Python 版エディタと相互運用確認)、(c) スライスごとの目視比較

- [x] スライス 1: widgets 基盤 (12ファイル) + App シェル + ImageEditor
  (canvas_panel / image_viewer。field_cursor は sound/music 用のためスライス 3 へ)
  (2026-07-18)。FFI に pyxel_colors_replace を追加 (パレット伸長は colors_set では
  不可能だった唯一の欠落)。未移植タブは PlaceholderEditor 表示。
  検証: EditorSmoke (headless 19 項目: パレット合成/鉛筆/undo/redo/カラーピッカー/
  ショートカット/Ctrl+S 保存/タブ切替、全パス) + ウィンドウ起動確認。目視比較は未
- [ ] スライス 1 残: Python 版エディタとの目視比較 (ユーザー確認待ち)
- [x] スライス 2: TilemapEditor (2026-07-19)。CanvasPanel を ICanvas&lt;TValue&gt; で
  ジェネリック化 (Color/Tile、Python のダックタイピング代替)、モード固有処理は
  ICanvasPanelHost&lt;TValue&gt; で各エディタへ。TilemapViewer + タイルスタンプ
  (EMPTY_TILE 番兵 + Python 式 Mod)。副産物: Pyxel.Load 時にバンクラッパーの
  キャッシュを無効化する修正 (load はバンクを差し替えるため旧ハンドルが陳腐化
  していた。Detach + Invalidate)。EditorSmoke 32 項目 (pyxres ラウンドトリップ
  含む) 全パス。目視比較は未
- [x] スライス 3: SoundEditor (2026-07-19)。field_cursor + piano_keyboard /
  piano_roll / octave_bar / sound_field。コピー交換配列 (sound.Notes 等) は
  FieldView (get→変更→set のリストビュー) で Python のライブリスト意味論を再現。
  speed_var の hasattr 判定は SetSpeedVar の遅延注入で代替。EditorSmoke 44 項目
  全パス (ピアノロール入力/undo/鍵盤+Enter/音色入力/speed/再生)。
  sound_selector は music 用のためスライス 4 へ
- [x] スライス 4: MusicEditor (music_field + sound_selector)。seqs は NUM_CHANNELS に
  正規化。PlaceholderEditor 撤去で 4 タブ全て実働。EditorSmoke 51 項目全パス (2026-07-19)
- [x] dotnet tool 化: PackAsTool (コマンド名 `pyxel-edit`、native dll + assets を
  tools/ に同梱) + Pack.ps1 組込 + README 更新。グローバルインストール →
  `pyxel-edit <file>` 起動まで検証済み (2026-07-19)
- [ ] 仕上げ: Python 版エディタとの目視比較 (ユーザー確認待ち)。
  自動テストで拾えない見た目・操作感・音の確認

## Stage 6: Web 対応 (2026-07-19 設計レビューで合意)

ゴール: 自作 PyxelSharp ゲームを `dotnet publish` で静的サイト化しブラウザで配布できる
(本家 app2html 相当)。前提知識: pyxel-core は emscripten 対応済み
(platform 層に emscripten_set_main_loop 分岐あり、本家 Web 版と同じコア)。

方針 (合意済み):
- **アプリ形態**: wasmbrowser (Microsoft.NET.Sdk.WebAssembly、UI フレームワークなし)。
  Rust クレートを wasm32-unknown-emscripten の staticlib でビルドし
  NativeFileReference でリンク。SDL2 は emscripten ポート (-sUSE_SDL=2) に切替
- **実行方式**: まずインタプリタ。30fps 未達なら AOT 検討。配布 15-30MB 許容
- **リスク方針**: スパイク先行。BouncingBall がブラウザで動く最小構成を go/no-go
  判断点にする。最大リスクは .NET wasm-tools の emscripten バージョンと
  Rust/SDL2 側の整合。失敗時は原因と代替案を持ち帰って再レビュー
- **スコープ**: デスクトップブラウザ (最新 Chrome/Edge) のみ。スマホ (タッチ/
  仮想ゲームパッド)・エディタ Web 版・Launcher/Code Maker 相当は対象外
- **検証/公開**: ローカル HTTP サーバで確認 → 本実装後 GitHub Pages に
  BouncingBall デモ公開
- ツールチェーン追加許可済み: wasm-tools/wasm-experimental ワークロード +
  rustup wasm32-unknown-emscripten + emsdk (専用ディレクトリに隔離、2-3GB)

- [x] スパイク: BouncingBall がブラウザで動作、**GO 判定** (2026-07-20)。
  確定ビルドレシピと解決した障害 6 件 (EH 衝突 / SDL2 ポート / wasm-opt /
  emcc シム / unwind / JS スタブ) は [WEB_DESIGN.md](WEB_DESIGN.md) §5 参照。
  canvas は CSS サイズ必須 (未指定だと ~1px に潰れる) も判明・解決済み。
  スパイク成果物は scratchpad のみ (リポジトリ未組込)

### 本実装の残作業 (スパイク成果のリポジトリ化)

検証残:
- [x] 可視タブでの目視確認: リロード後 640x480 でボール動作をユーザー確認 (2026-07-20)
- [ ] キーボード / マウス入力の動作確認 (MOUSE 座標表示をサンプルに追加済み。
  Pages デモで確認。非 US 配列の補正 `_scanCorrection` はスタブ [] のままで
  英字配列相当になる点に注意)
- [ ] 音声の動作確認 (WebAudio 経由。ブラウザの自動再生制限があるため
  「クリックで開始」ゲート等のユーザー操作トリガーが必要になる見込み)

リポジトリ組込 (2026-07-20 完了):
- [x] `csharp/samples/BouncingBall.Web`: 検証ハック除去 + レスポンシブ CSS
  (forceRaf はテスト補助として残置・文書化)。MOUSE 座標表示を追加
- [x] 追加の必須知見: **marshal-ilgen コンポーネント** — 既定の wasm リンクは
  スタブ版で bool 引数 P/Invoke (Pyxel.Mouse 等) が mono assert 死する。
  `<_MonoComponent Include="marshal-ilgen" />` で本物をリンク (csproj 参照)
- [x] emcc.exe シムを `tools/emcc-shim/` に収録 (EMSDK 環境変数参照に一般化)
- [x] `tools/Build-Wasm.ps1`: 前提チェック → embuilder → シム → cargo staticlib →
  publish の一発化。EmsdkRoot は EMSDK env / 引数化、csproj 側も
  `$(EmsdkRoot)` プロパティ化 (フォワードスラッシュ変換込み)
- [ ] Web ホスト資産 (index.html 雛形 / JS スタブ / main.js グルー) の共通化
  (props/targets or コンテンツ NuGet)。第 2 サンプル (JumpGame.Web) 追加時に実施

公開・文書 (2026-07-20 完了):
- [x] GitHub Pages デモ公開: https://clayandromeda.github.io/pyxel-sharp/
  (gh-pages ブランチに publish/wwwroot を配置、.nojekyll 付き)
- [x] README に Web セクション (前提・Build-Wasm.ps1・配布方法・デモ URL)
- [x] WEB_DESIGN.md を確定レシピで更新 (marshal-ilgen 含む)

品質・性能 (計測してから判断):
- [ ] 体感 fps 計測。30fps 未達なら `RunAOTCompilation=true` を試す (合意済み方針)
- [ ] 配布サイズ計測 (現状 ~30MB 非圧縮)。必要なら IL トリミング /
  `-O0` リンクの見直し (wasm-opt 回避策の解除が前提)
- [ ] Rust 側 wasm ビルドの安定化: nightly-2026-07-14 固定を rust-toolchain 等で
  明示 (build-std が nightly 依存のため)。将来 .NET の emscripten が更新されたら
  wasm-opt 回避と -O0 を解除して再計測

アセット対応 (実ゲームに必須、BouncingBall では未検証):
- [ ] .pyxres / 画像 / フォントのロード: emscripten 仮想 FS (MEMFS) への
  プリロード方法を確立 (dotnet.js の VFS 機能 or Module.preRun)。
  JumpGame.Web を第 2 サンプルとして移植して検証するのが望ましい

### 将来拡張 (Stage 6 スコープ外と合意済み)

- [ ] スマホ対応: タッチ操作・仮想ゲームパッド (本家 gamepad 実装 +
  `_readVirtualGamepadBitmask` の実装移植)
- [ ] `dotnet new pyxel-web` テンプレート (PyxelSharp.Templates への追加)
- [ ] エディタ (pyxel-edit) の Web 実行 (ファイル I/O の設計が別途必要)

## その他 (時期未定)

- [ ] NuGet.org 公開 (Stage 4 で「ローカルフィードでまず自分用」と合意した際の後回し分)。
  セットで: CI (GitHub Actions windows-latest: cargo + dotnet build + HeadlessSmoke +
  EditorSmoke + pack)、TFM の net8.0 (LTS) 引き下げ検討、パッケージ README 整備
- [ ] Linux / macOS ビルド対応 (pyxel-core は SDL2 なので原理的には可能。
  runtimes/ 構造はマルチプラットフォーム前提で設計しておく)
- [ ] Python サンプル (pyxel/python/pyxel/examples 01〜) の移植で網羅検証 (Stage 3 残)
- [ ] 本家 pyxel サブモジュールの更新運用 (key.rs 差分 → Generate-KeyEnum.ps1 再実行、
  エディタ移植の差分追従、web ビルドの emscripten バージョン整合の再確認)
