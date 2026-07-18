using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// tilemap_viewer.py
public sealed class TilemapViewer : Widget
{
    private readonly Image _tilemapImage = new(64, 64);
    private readonly WidgetVar<int> _tilemapIndexVar;
    private readonly WidgetVar<string> _helpMessageVar;

    public WidgetVar<int> FocusXVar { get; }
    public WidgetVar<int> FocusYVar { get; }

    public TilemapViewer(EditorBase parent)
        : base(parent, 157, 16, 66, 65)
    {
        _tilemapIndexVar = ((ITilemapHost)parent).TilemapIndexVar;
        _helpMessageVar = parent.HelpMessageVar;

        FocusXVar = new WidgetVar<int>(0);
        FocusXVar.AddSetFilter(value => Math.Clamp(value, 0, 30));

        FocusYVar = new WidgetVar<int>(0);
        FocusYVar.AddSetFilter(value => Math.Clamp(value, 0, 30));

        // Set event listeners
        MouseDown += OnMouseDown;
        MouseDrag += (key, x, y, _, _) => OnMouseDown(key, x, y);
        MouseHover += OnMouseHover;
        Update += OnUpdate;
        Draw += OnDraw;
    }

    // Helpers

    private (int X, int Y) ScreenToFocus(int x, int y)
    {
        var fx = Math.Clamp(EditorMath.FloorDiv(x - X - 1, 2), 0, 31);
        var fy = Math.Clamp(EditorMath.FloorDiv(y - Y - 1, 2), 0, 31);
        return (fx, fy);
    }

    // Event handlers

    private void OnMouseDown(Key key, int x, int y)
    {
        if (key == Key.MouseButtonLeft)
        {
            var (fx, fy) = ScreenToFocus(x, y);
            FocusXVar.Set(fx);
            FocusYVar.Set(fy);
        }
    }

    private void OnMouseHover(int x, int y)
    {
        var (fx, fy) = ScreenToFocus(x, y);
        _helpMessageVar.Set($"TARGET:CURSOR ({fx * 8},{fy * 8})");
    }

    private void OnUpdate()
    {
        var tilemap = Pyxel.Tilemaps[_tilemapIndexVar.Get()];
        var image = Pyxel.Images[tilemap.ImageSourceIndex ?? 0];

        // Refresh a slice of the preview each frame by sampling representative pixels.
        var startY = Pyxel.FrameCount % 8 * 8;

        for (var y = startY; y < startY + 8; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                var tile = tilemap.Pget(x * 4 + 1, y * 4 + 1);
                var col = image.Pget(tile.X * 8 + 3, tile.Y * 8 + 3);
                _tilemapImage.Pset(x, y, col);
            }
        }
    }

    private void OnDraw()
    {
        DrawPanel(X, Y, Width, Height);

        // Draw tilemap preview
        PyxelUser.UserPal();
        Pyxel.Blt(X + 1, Y + 1, _tilemapImage, 0, 0,
            _tilemapImage.Width, _tilemapImage.Height);
        Pyxel.Pal();

        // Draw focus outline
        var x = X + FocusXVar.Get() * 2 + 1;
        var y = Y + FocusYVar.Get() * 2 + 1;
        Pyxel.Clip(X + 1, Y + 1, Width - 2, Height - 2);
        Pyxel.Rectb(x, y, 4, 4, EditorSettings.PanelFocusColor);
        Pyxel.Rectb(x - 1, y - 1, 6, 6, EditorSettings.PanelFocusBorderColor);
        Pyxel.Clip();
    }
}
