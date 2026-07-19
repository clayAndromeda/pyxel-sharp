using System.Runtime.InteropServices.JavaScript;
using PyxelSharp;

// On browser-wasm the game is started from JS via GameEntry.Start, not from
// Main: emscripten_set_main_loop unwinds the stack with a JS exception, and
// going through dotnet's Main invocation would tear the main loop down.
Console.WriteLine("BouncingBall.Web: runtime started");

public static partial class GameEntry
{
    private const float Radius = 8;
    private static float _x = 80;
    private static float _y = 40;
    private static float _dx = 1.5f;
    private static float _dy = 1.2f;

    [JSExport]
    public static void Start()
    {
        Pyxel.Init(160, 120, title: "Pyxel-Sharp Bouncing Ball");
        Pyxel.Mouse(true);
        // Never returns on emscripten: the browser main loop takes over and
        // this call unwinds back to the JS caller.
        Pyxel.Run(Update, Draw);
    }

    private static void Update()
    {
        _x += _dx;
        _y += _dy;
        if (_x - Radius < 0 || _x + Radius > Pyxel.Width)
        {
            _dx = -_dx;
        }
        if (_y - Radius < 0 || _y + Radius > Pyxel.Height)
        {
            _dy = -_dy;
        }
    }

    private static void Draw()
    {
        Pyxel.Cls(Color.Navy);
        Pyxel.Text(4, 4, $"FRAME {Pyxel.FrameCount}", Color.White);
        Pyxel.Text(4, 12, $"MOUSE {Pyxel.MouseX},{Pyxel.MouseY}", Color.LightBlue);
        Pyxel.Circ(_x, _y, Radius, Color.Yellow);
        Pyxel.Circb(_x, _y, Radius, Color.Orange);
    }
}
