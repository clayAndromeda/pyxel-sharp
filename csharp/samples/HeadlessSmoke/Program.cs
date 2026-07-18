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

Console.WriteLine("HeadlessSmoke: all checks passed");
return 0;

static void Check(bool condition, string what)
{
    if (!condition)
    {
        Console.Error.WriteLine($"FAILED: {what}");
        Environment.Exit(1);
    }
    Console.WriteLine($"ok: {what}");
}
