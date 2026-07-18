// Port of the official 02_jump_game.py example.
using PyxelSharp;

new App();

class App
{
    private int _score;
    private float _playerX = 72;
    private float _playerY = -16;
    private float _playerDy;
    private bool _isAlive = true;

    private readonly (int X, int Y)[] _farCloud = [(-10, 75), (40, 65), (90, 60)];
    private readonly (int X, int Y)[] _nearCloud = [(10, 25), (70, 35), (120, 15)];
    private readonly (float X, float Y, bool IsAlive)[] _floor;
    private readonly (float X, float Y, int Kind, bool IsAlive)[] _fruit;

    public App()
    {
        Pyxel.Init(160, 120, title: "Pyxel Jump");
        Pyxel.Load(Path.Combine(AppContext.BaseDirectory, "assets", "jump_game.pyxres"));

        _floor = new (float, float, bool)[4];
        _fruit = new (float, float, int, bool)[4];
        for (var i = 0; i < 4; i++)
        {
            _floor[i] = (i * 60, Pyxel.Rndi(8, 104), true);
            _fruit[i] = (i * 60, Pyxel.Rndi(0, 104), Pyxel.Rndi(0, 2), true);
        }

        Pyxel.Playm(0, loop: true);
        Pyxel.Run(Update, Draw);
    }

    private void Update()
    {
        if (Pyxel.Btnp(Key.Q))
        {
            Pyxel.Quit();
        }

        UpdatePlayer();

        for (var i = 0; i < _floor.Length; i++)
        {
            _floor[i] = UpdateFloor(_floor[i]);
        }

        for (var i = 0; i < _fruit.Length; i++)
        {
            _fruit[i] = UpdateFruit(_fruit[i]);
        }
    }

    private void UpdatePlayer()
    {
        if (Pyxel.Btn(Key.Left) || Pyxel.Btn(Key.Gamepad1ButtonDpadLeft))
        {
            _playerX = Math.Max(_playerX - 2, 0);
        }
        if (Pyxel.Btn(Key.Right) || Pyxel.Btn(Key.Gamepad1ButtonDpadRight))
        {
            _playerX = Math.Min(_playerX + 2, Pyxel.Width - 16);
        }

        _playerY += _playerDy;
        _playerDy = Math.Min(_playerDy + 1, 8);

        if (_playerY > Pyxel.Height)
        {
            if (_isAlive)
            {
                _isAlive = false;
                Pyxel.Play(3, 5);
            }

            if (_playerY > 600)
            {
                _score = 0;
                _playerX = 72;
                _playerY = -16;
                _playerDy = 0;
                _isAlive = true;
            }
        }
    }

    private (float X, float Y, bool IsAlive) UpdateFloor((float X, float Y, bool IsAlive) floor)
    {
        var (x, y, isAlive) = floor;

        if (isAlive)
        {
            if (_playerX + 16 >= x
                && _playerX <= x + 40
                && _playerY + 16 >= y
                && _playerY <= y + 8
                && _playerDy > 0)
            {
                isAlive = false;
                _score += 10;
                _playerDy = -12;
                Pyxel.Play(3, 3);
            }
        }
        else
        {
            y += 6;
        }

        x -= 4;

        if (x < -40)
        {
            x += 240;
            y = Pyxel.Rndi(8, 104);
            isAlive = true;
        }

        return (x, y, isAlive);
    }

    private (float X, float Y, int Kind, bool IsAlive) UpdateFruit(
        (float X, float Y, int Kind, bool IsAlive) fruit)
    {
        var (x, y, kind, isAlive) = fruit;

        if (isAlive && Math.Abs(x - _playerX) < 12 && Math.Abs(y - _playerY) < 12)
        {
            isAlive = false;
            _score += (kind + 1) * 100;
            _playerDy = Math.Min(_playerDy, -8);
            Pyxel.Play(3, 4);
        }

        x -= 2;

        if (x < -40)
        {
            x += 240;
            y = Pyxel.Rndi(0, 104);
            kind = Pyxel.Rndi(0, 2);
            isAlive = true;
        }

        return (x, y, kind, isAlive);
    }

    private void Draw()
    {
        Pyxel.Cls(12);

        // Draw sky
        Pyxel.Blt(0, 88, 0, 0, 88, 160, 32);

        // Draw mountain
        Pyxel.Blt(0, 88, 0, 0, 64, 160, 24, 12);

        // Draw trees
        var offset = Pyxel.FrameCount % 160;
        for (var i = 0; i < 2; i++)
        {
            Pyxel.Blt(i * 160 - offset, 104, 0, 0, 48, 160, 16, 12);
        }

        // Draw clouds
        offset = Pyxel.FrameCount / 16 % 160;
        for (var i = 0; i < 2; i++)
        {
            foreach (var (x, y) in _farCloud)
            {
                Pyxel.Blt(x + i * 160 - offset, y, 0, 64, 32, 32, 8, 12);
            }
        }

        offset = Pyxel.FrameCount / 8 % 160;
        for (var i = 0; i < 2; i++)
        {
            foreach (var (x, y) in _nearCloud)
            {
                Pyxel.Blt(x + i * 160 - offset, y, 0, 0, 32, 56, 8, 12);
            }
        }

        // Draw floors
        foreach (var (x, y, _) in _floor)
        {
            Pyxel.Blt(x, y, 0, 0, 16, 40, 8, 12);
        }

        // Draw fruits
        foreach (var (x, y, kind, isAlive) in _fruit)
        {
            if (isAlive)
            {
                Pyxel.Blt(x, y, 0, 32 + kind * 16, 0, 16, 16, 12);
            }
        }

        // Draw player
        Pyxel.Blt(_playerX, _playerY, 0, _playerDy > 0 ? 16 : 0, 0, 16, 16, 12);

        // Draw score
        var score = $"SCORE {_score,4}";
        Pyxel.Text(5, 4, score, 1);
        Pyxel.Text(4, 4, score, 7);
    }
}
