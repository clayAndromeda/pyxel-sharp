# Pyxel-Sharp TODO

ステージ全体像は [DESIGN.md](DESIGN.md) の「ステージ計画」を参照。ここでは具体的な作業項目を管理する。

## Stage 2: グラフィックスリソース (次にやる)

進め方は縦切り: まず Image + blt + サンプル動作確認を完結させ、確立したハンドル
パターンを Tilemap / Font に横展開する (2026-07-18 合意)。

### スライス 1: Image + blt (着手中)

- [ ] `Image` のオペークハンドル化 (`Box::into_raw` + C# ラッパークラス)
  - pyxel-binding の `image_wrapper.rs` が参考。`SharedImage` をハンドルで包む
  - `Pyxel.Images[i]` (イメージバンク)、`Pyxel.Screen` は static キャッシュでアプリ寿命
  - `new Image(w, h)` は IDisposable
- [ ] 解放は drop キュー方式: Run を呼んだメインスレッドからの Dispose は即時 drop、
  別スレッド / ファイナライザは ConcurrentQueue に積み、毎フレーム update
  コールバック先頭で消化
- [ ] Image インスタンスの描画 API フルセット (image_wrapper.rs 相当:
  Pget/Pset/Load/Save/Cls/Line/Rect/Text/Blt 等)
- [ ] `Pyxel.Blt` (スクリーンへの転送。rotate/scale/colkey 含む。
  int バンク番号 / Image のオーバーロード)
- [ ] FFI 境界のパニック対策: 共通マクロで `catch_unwind` ラップ →
  i32 + last_error 規約で PyxelException 化。Stage 1 の既存 extern 関数も同時改修
- [ ] 検証: 本家 01_hello_pyxel 移植サンプル (ロゴ PNG はサブモジュール内アセット参照)
  + HeadlessSmoke に Image 系検証を追加

### スライス 2: 横展開

- [ ] `Tilemap` のオペークハンドル化 + `bltm` (`ImageSource` の表現を検討)
- [ ] `Font` (`new Font(path)` + `Pyxel.Text` の font オプション引数)
- [ ] リソースファイル: `Load` (.pyxres)、`user_data_dir` 等
- [ ] 残りの graphics API の棚卸し (`fill` など Pyxel struct に無い API の所在確認)

## Stage 3: audio / math (Python 版フルパリティ)

- [ ] `Sound` / `Music` / `Channel` / `Tone` のハンドル化
- [ ] `Play` / `Playm` / `Stop` / MML (`sound.Mml(...)`)
- [ ] math モジュール (`Rndi` / `Rndf` / `Noise` / `Atan2` 等)
- [ ] 入力の残り: `input_text` / `dropped_files`
- [ ] Python サンプル (pyxel/python/pyxel/examples 01〜) の移植で網羅検証

## インフラ / 配布

- [ ] `git push` (origin = github.com/clayAndromeda/pyxel-sharp、未 push)
- [ ] CI (GitHub Actions windows-latest): cargo build + dotnet build + HeadlessSmoke 実行
- [ ] NuGet パッケージ化 (`runtimes/win-x64/native/pyxel_bind_cs.dll` 同梱)
- [ ] Linux / macOS ビルド対応 (pyxel-core は SDL2 なので原理的には可能)
