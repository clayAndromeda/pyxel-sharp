// FFI smoke test: runs pyxel-core headless (no window) and verifies that
// drawing and reading pixels round-trips through the binding.
using PyxelSharp;

Pyxel.Init(64, 48, title: "smoke", headless: true);

Check(Pyxel.Width == 64, $"Width = {Pyxel.Width}");
Check(Pyxel.Height == 48, $"Height = {Pyxel.Height}");

Pyxel.Cls(Color.Navy);
Check(Pyxel.Pget(10, 10) == Color.Navy, $"cls -> pget = {Pyxel.Pget(10, 10)}");

Pyxel.Rect(5, 5, 10, 10, Color.Red);
Check(Pyxel.Pget(7, 7) == Color.Red, $"rect -> pget = {Pyxel.Pget(7, 7)}");
Check(Pyxel.Pget(20, 20) == Color.Navy, $"outside rect -> pget = {Pyxel.Pget(20, 20)}");

Pyxel.Pset(0, 0, Color.White);
Check(Pyxel.Pget(0, 0) == Color.White, $"pset -> pget = {Pyxel.Pget(0, 0)}");

Pyxel.Text(2, 20, "OK", Color.Yellow);
Check(!Pyxel.Btn(Key.Space), "btn(Space) is false");

// Stage 2: image handles

Check(Pyxel.Screen.Width == 64, $"Screen.Width = {Pyxel.Screen.Width}");
Check(Pyxel.Screen.Pget(0, 0) == Color.White, $"Screen.Pget = {Pyxel.Screen.Pget(0, 0)}");

var image = new Image(8, 8);
Check(image.Width == 8 && image.Height == 8, $"new Image -> {image.Width}x{image.Height}");
image.Cls(Color.Green);
image.Pset(1, 1, Color.Yellow);
Check(image.Pget(1, 1) == Color.Yellow, $"image pset -> pget = {image.Pget(1, 1)}");
Check(image.Data[image.Width + 1] == Color.Yellow.Value, "image Data span matches pset");

Pyxel.Blt(30, 30, image, 0, 0, 8, 8);
Check(Pyxel.Pget(31, 31) == Color.Yellow, $"blt -> screen pget = {Pyxel.Pget(31, 31)}");

// colkey masks out matching pixels; (45, 34) keeps the earlier Navy clear
Pyxel.Blt(44, 30, image, 0, 0, 8, 8, colorKey: Color.Green);
Check(Pyxel.Pget(45, 34) == Color.Navy, $"blt colkey -> screen pget = {Pyxel.Pget(45, 34)}");
Pyxel.Blt(20, 5, image, 0, 0, 8, 8, rotate: 90, scale: 1.5f);

Check(Pyxel.Images.Count == 3, $"Images.Count = {Pyxel.Images.Count}");
Pyxel.Images[0].Set(0, 0, "0123", "4567");
Check(Pyxel.Images[0].Pget(2, 0) == 2, $"bank set -> pget = {Pyxel.Images[0].Pget(2, 0)}");
Check(Pyxel.Images[0].Pget(3, 1) == 7, $"bank set -> pget = {Pyxel.Images[0].Pget(3, 1)}");
Check(ReferenceEquals(Pyxel.Images[0], Pyxel.Images[0]), "bank wrappers are cached");

// save -> load round-trip without external assets
var savePath = Path.Combine(Path.GetTempPath(), "pyxel_smoke.png");
image.Save(savePath);
var loaded = Image.FromImage(savePath);
Check(loaded.Width == 8 && loaded.Height == 8, $"FromImage -> {loaded.Width}x{loaded.Height}");
Check(loaded.Pget(1, 1) == Color.Yellow, $"FromImage -> pget = {loaded.Pget(1, 1)}");
Pyxel.Images[1].Load(0, 0, savePath);
Check(Pyxel.Images[1].Pget(1, 1) == Color.Yellow, $"bank load -> pget = {Pyxel.Images[1].Pget(1, 1)}");
loaded.Dispose();
File.Delete(savePath);

// error paths: engine errors and pyxel-core panics both become PyxelException
try
{
    Pyxel.Images[0].Load(0, 0, "no_such_file.png");
    Check(false, "load of missing file should throw");
}
catch (PyxelException exception)
{
    Check(true, $"missing file -> PyxelException ({Truncate(exception.Message)})");
}
try
{
    image.Blt(0, 0, image, 0, 0, 4, 4); // RefCell double borrow panics in pyxel-core
    Check(false, "self-blt should throw");
}
catch (PyxelException exception)
{
    Check(true, $"panic -> PyxelException ({Truncate(exception.Message)})");
}

image.Dispose();
try
{
    image.Pget(0, 0);
    Check(false, "disposed image should throw");
}
catch (ObjectDisposedException)
{
    Check(true, "disposed image -> ObjectDisposedException");
}

Console.WriteLine("HeadlessSmoke: all checks passed");
return 0;

static string Truncate(string message) =>
    message.Length <= 60 ? message : message[..60] + "...";

static void Check(bool condition, string what)
{
    if (!condition)
    {
        Console.Error.WriteLine($"FAILED: {what}");
        Environment.Exit(1);
    }
    Console.WriteLine($"ok: {what}");
}
