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

## Stage 5: リソースエディタ C# 移植 (Stage 4 の後)

- [ ] pyxel/python/pyxel/editor (約20ファイル) を PyxelSharp 上に移植。
  PyxelSharp 自身のドッグフーディングを兼ねる。詳細設計は着手時に別途レビュー
- それまでの .pyxres 編集は Python 版 pyxel のエディタを併用 (フォーマット共通)

## その他 (時期未定)

- [ ] Linux / macOS ビルド対応 (pyxel-core は SDL2 なので原理的には可能。
  runtimes/ 構造はマルチプラットフォーム前提で設計しておく)
- [ ] Python サンプル (pyxel/python/pyxel/examples 01〜) の移植で網羅検証 (Stage 3 残)
