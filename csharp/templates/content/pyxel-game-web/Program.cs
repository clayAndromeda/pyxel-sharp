using System.Runtime.InteropServices.JavaScript;
using PyxelSharp;

// On browser-wasm the game is started from JS via GameEntry.Start (see
// wwwroot/index.html + pyxel-boot.js), not from Main: emscripten's main loop
// unwinds the stack, which would tear the runtime down if entered via Main.
Console.WriteLine("PyxelGameWeb: runtime started");

public static partial class GameEntry
{
    private static float _x = 80, _dx = 1.5f;

    [JSExport]
    public static void Start()
    {
        Pyxel.Init(160, 120, title: "PyxelGameWeb");
        // Never returns on emscripten: the browser main loop takes over.
        Pyxel.Run(Update, Draw);
    }

    private static void Update()
    {
        _x += _dx;
        if (_x < 8 || _x > Pyxel.Width - 8) _dx = -_dx;
    }

    private static void Draw()
    {
        Pyxel.Cls(Color.Navy);
        Pyxel.Circ(_x, 60, 8, Color.Yellow);
    }
}
