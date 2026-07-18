using PyxelSharp;

const float Radius = 8;
float x = 80, y = 40;
float dx = 1.5f, dy = 1.2f;

Pyxel.Init(160, 120, title: "Pyxel-Sharp Bouncing Ball", quitKey: Key.Q);
Pyxel.Run(Update, Draw);

void Update()
{
    x += dx;
    y += dy;
    if (x - Radius < 0 || x + Radius > Pyxel.Width)
    {
        dx = -dx;
    }
    if (y - Radius < 0 || y + Radius > Pyxel.Height)
    {
        dy = -dy;
    }
}

void Draw()
{
    Pyxel.Cls(Color.Navy);
    Pyxel.Text(4, 4, $"FRAME {Pyxel.FrameCount}", Color.White);
    Pyxel.Circ(x, y, Radius, Color.Yellow);
    Pyxel.Circb(x, y, Radius, Color.Orange);
}
