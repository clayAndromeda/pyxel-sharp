using PyxelSharp;

float x = 80, dx = 1.5f;

Pyxel.Init(160, 120, title: "PyxelGame", quitKey: Key.Q);
Pyxel.Run(Update, Draw);

void Update()
{
    x += dx;
    if (x < 8 || x > Pyxel.Width - 8) dx = -dx;
}

void Draw()
{
    Pyxel.Cls(Color.Navy);
    Pyxel.Circ(x, 60, 8, Color.Yellow);
}
