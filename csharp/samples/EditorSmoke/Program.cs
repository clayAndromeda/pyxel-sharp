// Editor slice 1 smoke test: drives the real App headless with injected
// input (SetBtn/SetMousePos) and verifies widget behavior end-to-end.
using PyxelSharp;
using PyxelSharp.Editor;

var failures = 0;

void Check(bool condition, string label)
{
    Console.WriteLine($"{(condition ? "ok" : "NG")}: {label}");
    if (!condition)
    {
        failures++;
    }
}

var resPath = Path.Combine(Path.GetTempPath(), "editor_smoke.pyxres");
File.Delete(resPath);

var app = new App(resPath, headless: true);

void Frame()
{
    app.UpdateAll();
    app.DrawAll();
    Pyxel.Flip();
}

// Startup: palette extended to system 16 + user 16 (default palette, no file)
Check(Pyxel.Width == 240 && Pyxel.Height == 180, $"app size {Pyxel.Width}x{Pyxel.Height}");
Check(PyxelUser.NumUserColors == 16, $"num_user_colors = {PyxelUser.NumUserColors}");
Check(Pyxel.Colors.Count == 32, $"palette entries = {Pyxel.Colors.Count}");

var imageEditor = (ImageEditor)app.Editors[0];
Check(imageEditor.IsVisible, "image editor visible at start");
Check(imageEditor.ColorVar.Get() == 7, $"initial color = {imageEditor.ColorVar.Get()}");
Check(imageEditor.ToolVar.Get() == EditorSettings.ToolPencil, "initial tool = pencil");

// First frame draws the chrome
Frame();
Check(Pyxel.Pget(120, 5) == (Color)1, $"top panel color = {Pyxel.Pget(120, 5)}");

// Pencil: click canvas cell (2,3) -> bank 0 pixel (2,3) becomes color 7
Check(Pyxel.Images[0].Pget(2, 3) == (Color)0, "bank pixel initially 0");
Pyxel.SetMousePos(32, 45);
Frame();
Pyxel.SetBtn(Key.MouseButtonLeft, true);
Frame();
Pyxel.SetBtn(Key.MouseButtonLeft, false);
Frame();
Check(Pyxel.Images[0].Pget(2, 3) == (Color)7, $"pencil committed: {Pyxel.Images[0].Pget(2, 3)}");

// Undo / redo
Check(imageEditor.CanUndo, "undo available after pencil");
imageEditor.Undo();
Check(Pyxel.Images[0].Pget(2, 3) == (Color)0, "undo reverts pixel");
Check(imageEditor.CanRedo, "redo available after undo");
imageEditor.Redo();
Check(Pyxel.Images[0].Pget(2, 3) == (Color)7, "redo reapplies pixel");

// Color picker: click cell index 2 (16 user colors -> 8x8 cells, 8 per row)
Pyxel.SetMousePos(30, 160);
Pyxel.SetBtn(Key.MouseButtonLeft, true);
Frame();
Pyxel.SetBtn(Key.MouseButtonLeft, false);
Frame();
Check(imageEditor.ColorVar.Get() == 2, $"color picker click -> {imageEditor.ColorVar.Get()}");

// Color shortcut: key 4 selects color 3
Pyxel.SetBtn(Key.D4, true);
Frame();
Pyxel.SetBtn(Key.D4, false);
Frame();
Check(imageEditor.ColorVar.Get() == 3, $"color shortcut 4 -> {imageEditor.ColorVar.Get()}");

// Tool shortcut: R selects RECTB
Pyxel.SetBtn(Key.R, true);
Frame();
Pyxel.SetBtn(Key.R, false);
Frame();
Check(imageEditor.ToolVar.Get() == EditorSettings.ToolRectb, "tool shortcut R -> rectb");

// Ctrl+S saves the resource file
Pyxel.SetBtn(Key.Ctrl, true);
Pyxel.SetBtn(Key.S, true);
Frame();
Pyxel.SetBtn(Key.S, false);
Pyxel.SetBtn(Key.Ctrl, false);
Frame();
Check(File.Exists(resPath), "ctrl+s saved the .pyxres file");

// Alt+Right switches to the next editor tab
Pyxel.SetBtn(Key.Alt, true);
Pyxel.SetBtn(Key.Right, true);
Frame();
Pyxel.SetBtn(Key.Right, false);
Pyxel.SetBtn(Key.Alt, false);
Frame();
Check(app.EditorTypeVar.Get() == 1, $"alt+right -> editor {app.EditorTypeVar.Get()}");
Check(!imageEditor.IsVisible && app.Editors[1].IsVisible, "editor visibility switched");

// --- Slice 2: tilemap editor ---
var tilemapEditor = (TilemapEditor)app.Editors[1];
Check(tilemapEditor.ImageIndexVar.Get() == 0, "tilemap imgsrc picker starts at 0");

// Pick tile (2,1) in the image viewer (tilemap mode: viewer at 157,80)
Pyxel.SetMousePos(178, 93);
Pyxel.SetBtn(Key.MouseButtonLeft, true);
Frame();
Pyxel.SetBtn(Key.MouseButtonLeft, false);
Frame();
Check(tilemapEditor.TileXVar.Get() == 2 && tilemapEditor.TileYVar.Get() == 1,
    $"tile pick -> ({tilemapEditor.TileXVar.Get()},{tilemapEditor.TileYVar.Get()})");

// Pencil: stamp the tile onto tilemap cell (0,0)
Check(Pyxel.Tilemaps[0].Pget(0, 0) == new Tile(0, 0), "tilemap cell initially (0,0)");
Pyxel.SetMousePos(16, 21);
Pyxel.SetBtn(Key.MouseButtonLeft, true);
Frame();
Pyxel.SetBtn(Key.MouseButtonLeft, false);
Frame();
Check(Pyxel.Tilemaps[0].Pget(0, 0) == new Tile(2, 1),
    $"tile stamped: {Pyxel.Tilemaps[0].Pget(0, 0)}");

// Undo / redo
Check(tilemapEditor.CanUndo, "tilemap undo available");
tilemapEditor.Undo();
Check(Pyxel.Tilemaps[0].Pget(0, 0) == new Tile(0, 0), "tilemap undo reverts");
tilemapEditor.Redo();
Check(Pyxel.Tilemaps[0].Pget(0, 0) == new Tile(2, 1), "tilemap redo reapplies");

// Image source picker drives tilemap.imgsrc
tilemapEditor.ImageIndexVar.Set(1);
Check(Pyxel.Tilemaps[0].ImageSourceIndex == 1, "imgsrc picker -> tilemap.imgsrc");

// Save/load roundtrip through the .pyxres file
Pyxel.SetBtn(Key.Ctrl, true);
Pyxel.SetBtn(Key.S, true);
Frame();
Pyxel.SetBtn(Key.S, false);
Pyxel.SetBtn(Key.Ctrl, false);
Frame();
Pyxel.Tilemaps[0].Pset(5, 5, (9, 9));
Pyxel.Load(resPath);
Check(Pyxel.Tilemaps[0].Pget(0, 0) == new Tile(2, 1), "pyxres roundtrip keeps tile");
Check(Pyxel.Tilemaps[0].Pget(5, 5) == new Tile(0, 0), "pyxres roundtrip resets later edit");
Check(Pyxel.Tilemaps[0].ImageSourceIndex == 1, "pyxres roundtrip keeps imgsrc");

File.Delete(resPath);

if (failures > 0)
{
    Console.WriteLine($"{failures} CHECK(S) FAILED");
    Environment.Exit(1);
}
Console.WriteLine("ALL EDITOR SMOKE CHECKS PASSED");
