// Port of the official 01_hello_pyxel.py example.
using PyxelSharp;

Pyxel.Init(160, 120, title: "Hello Pyxel");
Pyxel.Images[0].Load(0, 0, Path.Combine(AppContext.BaseDirectory, "assets", "pyxel_logo_38x16.png"));
Pyxel.Run(Update, Draw);

static void Update()
{
    if (Pyxel.Btnp(Key.Q))
    {
        Pyxel.Quit();
    }
}

static void Draw()
{
    Pyxel.Cls(0);
    Pyxel.Text(55, 41, "Hello, Pyxel!", Pyxel.FrameCount % 16);
    Pyxel.Blt(61, 66, 0, 0, 0, 38, 16);
}
