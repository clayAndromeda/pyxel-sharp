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

### スライス 2: 横展開

- [ ] `Tilemap` のオペークハンドル化 + `bltm` (`ImageSource` の表現を検討)
- [ ] `Font` (`new Font(path)` + `Pyxel.Text` / `image.Text` の font オプション引数)
- [ ] `blt3d` / `bltm3d` (Image には未実装のまま。Tilemap と同時に)
- [ ] リソースファイル: `Load` (.pyxres)、`user_data_dir` 等
- [ ] 残りの graphics API の棚卸し (Python モジュール直下と Pyxel struct の差分確認)

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
