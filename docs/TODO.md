# Pyxel-Sharp TODO

ステージ全体像は [DESIGN.md](DESIGN.md) の「ステージ計画」を参照。ここでは具体的な作業項目を管理する。

## Stage 2: グラフィックスリソース (次にやる)

- [ ] `Image` のオペークハンドル化 (`Box::into_raw` + C# ラッパークラス)
  - pyxel-binding の `image_wrapper.rs` が参考。`RcImage` をハンドルで包む
  - `Pyxel.Images[i]` (イメージバンク)、`Pyxel.Screen`、`new Image(w, h)`
  - `image.Pget/Pset/Load/...`
- [ ] `blt` / `bltm` (スクリーンへの転送。回転・拡縮引数 rotate/scale 含む)
- [ ] `Tilemap` のオペークハンドル化 (`ImageSource` の表現を検討)
- [ ] `Font` (`new Font(path)` + `Pyxel.Text` の font オプション引数)
- [ ] リソースファイル: `Load` (.pyxres)、`user_data_dir` 等
- [ ] 残りの graphics API の棚卸し (`fill` など Pyxel struct に無い API の所在確認)
- [ ] FFI 境界のパニック対策: `catch_unwind` でラップして PyxelException に変換
  (現状 pyxel-core 内の assert は abort になる)

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

## 書き味の改善 (バインディング完成後に検討)

- [ ] ECS レイヤ (`PyxelSharp.Ecs`, Friflo.Engine.ECS ベース) — 旧セッションで方針検討済み。
      採用可否は Stage 2〜3 完了後に再判断

## 開発環境の注意 (このマシン固有)

- Smart App Control が未署名ビルド成果物をブロックする。無効化するか、
  detached プロセス経由でビルドすること (詳細は docs/DESIGN.md と Claude メモリ)
