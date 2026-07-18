using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// canvas_panel.py
//
// Generic over the cell value (Color for ImageEditor, Tile for TilemapEditor);
// mode-specific behavior goes through ICanvasPanelHost<TValue>.
internal sealed class CanvasPanel<TValue> : Widget
{
    private readonly ICanvasPanelHost<TValue> _host;
    private readonly WidgetVar<string> _helpMessageVar;
    private readonly ICanvas<TValue> _editCanvas;
    private readonly ScrollBar _hScrollBar;
    private readonly ScrollBar _vScrollBar;

    private int _pressX;
    private int _pressY;
    private int _lastX;
    private int _lastY;
    private int _dragOffsetX;
    private int _dragOffsetY;
    private int _selectX1;
    private int _selectY1;
    private int _selectX2;
    private int _selectY2;
    private TValue[,]? _canvasBuffer;
    private bool _isDragged;
    private bool _isAssistMode;

    public CanvasPanel(EditorBase parent)
        : base(parent, 11, 16, 130, 130)
    {
        _host = (ICanvasPanelHost<TValue>)parent;
        _helpMessageVar = parent.HelpMessageVar;
        _editCanvas = _host.CreateEditCanvas();

        _hScrollBar = new ScrollBar(this, 0, 129, scrollAmount: 32, sliderAmount: 2,
            value: 0, width: 130);
        _hScrollBar.Change += value => _host.FocusXVar.Set(value);
        _host.FocusXVar.Changed += value => _hScrollBar.ValueVar.Set(value);

        _vScrollBar = new ScrollBar(this, 129, 0, scrollAmount: 32, sliderAmount: 2,
            value: 0, height: 130);
        _vScrollBar.Change += value => _host.FocusYVar.Set(value);
        _host.FocusYVar.Changed += value => _vScrollBar.ValueVar.Set(value);

        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
        MouseDrag += OnMouseDrag;
        MouseHover += OnMouseHover;
        Update += OnUpdate;
        Draw += OnDraw;
    }

    // Helpers

    private ICanvas<TValue> Canvas => _host.Canvas;
    private int FocusX => _host.FocusXVar.Get();
    private int FocusY => _host.FocusYVar.Get();
    private int Tool => _host.ToolVar.Get();

    private (int X, int Y) ScreenToFocus(int x, int y)
    {
        var fx = Math.Clamp(EditorMath.FloorDiv(x - X - 1, 8), 0, 15);
        var fy = Math.Clamp(EditorMath.FloorDiv(y - Y - 1, 8), 0, 15);
        return (fx, fy);
    }

    private (int X, int Y, int W, int H) SelectionRect()
    {
        var x = FocusX * 8 + _selectX1;
        var y = FocusY * 8 + _selectY1;
        var w = _selectX2 - _selectX1 + 1;
        var h = _selectY2 - _selectY1 + 1;
        return (x, y, w, h);
    }

    private void ResetEditCanvas()
    {
        _editCanvas.Blt(0, 0, Canvas, FocusX * 8, FocusY * 8, 16, 16);
        _editCanvas.CopySourceFrom(Canvas);
    }

    private void FinishEditCanvas() => _host.FinishEditCanvas(_editCanvas, _pressX, _pressY);

    // Event handlers

    private void OnMouseDown(Key key, int x, int y)
    {
        // Right click picks the current color or tile.
        if (key == Key.MouseButtonRight)
        {
            var (fx, fy) = ScreenToFocus(x, y);
            fx += FocusX * 8;
            fy += FocusY * 8;
            _host.PickValue(Canvas.Pget(fx, fy));
            return;
        }
        if (key != Key.MouseButtonLeft)
        {
            return;
        }

        (x, y) = ScreenToFocus(x, y);
        _pressX = _lastX = x;
        _pressY = _lastY = y;
        _isDragged = true;
        _isAssistMode = false;

        // SELECT: begin selection
        if (Tool == EditorSettings.ToolSelect)
        {
            ResetEditCanvas();
            _selectX1 = _selectX2 = x;
            _selectY1 = _selectY2 = y;
        }

        // PENCIL/RECTB/RECT/CIRCB/CIRC: place initial dot
        else if (Tool is >= EditorSettings.ToolPencil and <= EditorSettings.ToolCirc)
        {
            ResetEditCanvas();
            _editCanvas.Pset(x, y, _host.DrawValue);
            FinishEditCanvas();
        }

        // BUCKET: flood fill and commit immediately
        else if (Tool == EditorSettings.ToolBucket)
        {
            _host.AddPreHistory();
            ResetEditCanvas();
            _editCanvas.Fill(x, y, _host.DrawValue);
            FinishEditCanvas();
            Canvas.Blt(FocusX * 8, FocusY * 8, _editCanvas, 0, 0, 16, 16);
            _host.AddPostHistory();
        }
    }

    private void OnMouseUp(Key key, int x, int y)
    {
        if (key != Key.MouseButtonLeft)
        {
            return;
        }

        _isDragged = false;
        if (Tool is >= EditorSettings.ToolPencil and <= EditorSettings.ToolCirc)
        {
            _host.AddPreHistory();
            Canvas.Blt(FocusX * 8, FocusY * 8, _editCanvas, 0, 0, 16, 16);
            _host.AddPostHistory();
        }
    }

    private void OnMouseDrag(Key key, int x, int y, int dx, int dy)
    {
        // Apply the active tool across the drag
        if (key == Key.MouseButtonLeft)
        {
            var x1 = _pressX;
            var y1 = _pressY;
            var x2 = EditorMath.FloorDiv(x - X - 1, 8);
            var y2 = EditorMath.FloorDiv(y - Y - 1, 8);

            if (Tool is >= EditorSettings.ToolRectb and <= EditorSettings.ToolCirc
                && _isAssistMode)
            {
                var adx = x2 - x1;
                var ady = y2 - y1;
                if (Math.Abs(adx) > Math.Abs(ady))
                {
                    y2 = y1 + Math.Abs(adx) * (ady > 0 ? 1 : -1);
                }
                else
                {
                    x2 = x1 + Math.Abs(ady) * (adx > 0 ? 1 : -1);
                }
            }

            // SELECT: update selection rectangle
            if (Tool == EditorSettings.ToolSelect)
            {
                x2 = Math.Clamp(x2, 0, 15);
                y2 = Math.Clamp(y2, 0, 15);
                (_selectX1, _selectX2) = x1 < x2 ? (x1, x2) : (x2, x1);
                (_selectY1, _selectY2) = y1 < y2 ? (y1, y2) : (y2, y1);
            }

            // PENCIL: freehand or assisted straight line
            else if (Tool == EditorSettings.ToolPencil)
            {
                if (_isAssistMode)
                {
                    ResetEditCanvas();
                    _editCanvas.Line(x1, y1, x2, y2, _host.DrawValue);
                    FinishEditCanvas();
                }
                else
                {
                    _editCanvas.Line(_lastX, _lastY, x2, y2, _host.DrawValue);
                    FinishEditCanvas();
                }
            }

            // RECTB: outlined rectangle
            else if (Tool == EditorSettings.ToolRectb)
            {
                ResetEditCanvas();
                _editCanvas.Rectb2(x1, y1, x2, y2, _host.DrawValue);
                FinishEditCanvas();
            }

            // RECT: filled rectangle
            else if (Tool == EditorSettings.ToolRect)
            {
                ResetEditCanvas();
                _editCanvas.Rect2(x1, y1, x2, y2, _host.DrawValue);
                FinishEditCanvas();
            }

            // CIRCB: outlined ellipse
            else if (Tool == EditorSettings.ToolCircb)
            {
                ResetEditCanvas();
                _editCanvas.Ellib2(x1, y1, x2, y2, _host.DrawValue);
                FinishEditCanvas();
            }

            // CIRC: filled ellipse
            else if (Tool == EditorSettings.ToolCirc)
            {
                ResetEditCanvas();
                _editCanvas.Elli2(x1, y1, x2, y2, _host.DrawValue);
                FinishEditCanvas();
            }

            _lastX = x2;
            _lastY = y2;
        }
        else if (key == Key.MouseButtonRight)
        {
            _dragOffsetX -= dx;
            _dragOffsetY -= dy;

            if (Math.Abs(_dragOffsetX) >= 16)
            {
                var offset = EditorMath.FloorDiv(_dragOffsetX, 16);
                _host.FocusXVar.Set(FocusX + offset);
                _dragOffsetX -= offset * 16;
            }
            if (Math.Abs(_dragOffsetY) >= 16)
            {
                var offset = EditorMath.FloorDiv(_dragOffsetY, 16);
                _host.FocusYVar.Set(FocusY + offset);
                _dragOffsetY -= offset * 16;
            }
        }
    }

    private void OnMouseHover(int x, int y)
    {
        var s = Tool == EditorSettings.ToolSelect ? "COPY:CTRL+C/X/V FLIP:H/V"
            : _isDragged ? "ASSIST:SHIFT"
            : "PICK:R-CLICK VIEW:R-DRAG";

        var (fx, fy) = ScreenToFocus(x, y);
        fx += FocusX * 8;
        fy += FocusY * 8;
        _helpMessageVar.Set($"{s} ({fx},{fy})");
    }

    private void OnUpdate()
    {
        if (_isDragged && !_isAssistMode && Pyxel.Btn(Key.Shift))
        {
            _isAssistMode = true;
            OnMouseDrag(Key.MouseButtonLeft, Pyxel.MouseX, Pyxel.MouseY, 0, 0);
        }

        // Copy/cut/paste bank (Ctrl+Shift or Cmd+Shift)
        var hasCmdOrCtrl = Pyxel.Btn(Key.Ctrl) || Pyxel.Btn(Key.Gui);
        if (Pyxel.Btn(Key.Shift) && hasCmdOrCtrl)
        {
            // Ctrl+Shift+C/Ctrl+Shift+X: Copy bank
            if (Pyxel.Btnp(Key.C) || Pyxel.Btnp(Key.X))
            {
                _host.BankClipboardCopy();
            }

            // Ctrl+Shift+X: Cut bank
            if (Pyxel.Btnp(Key.X))
            {
                _host.BankClipboardCut();
            }

            // Ctrl+Shift+V: Paste bank
            if (Pyxel.Btnp(Key.V))
            {
                _host.BankClipboardPaste();
            }
        }

        // Copy/cut/paste canvas (Ctrl/Cmd without Shift)
        if (Tool == EditorSettings.ToolSelect && !Pyxel.Btn(Key.Shift) && hasCmdOrCtrl)
        {
            // Ctrl+A: Select all
            if (Pyxel.Btnp(Key.A))
            {
                _selectX1 = _selectY1 = 0;
                _selectX2 = _selectY2 = 15;
            }

            // Ctrl+C: Copy
            if (Pyxel.Btnp(Key.C))
            {
                var (x, y, w, h) = SelectionRect();
                _canvasBuffer = Canvas.GetSlice(x, y, w, h);
            }

            // Ctrl+X: Cut
            if (Pyxel.Btnp(Key.X))
            {
                var (x, y, w, h) = SelectionRect();
                _canvasBuffer = Canvas.GetSlice(x, y, w, h);
                _host.AddPreHistory();
                Canvas.Rect(x, y, w, h, _host.EraseValue);
                _host.AddPostHistory();
            }

            // Ctrl+V: Paste
            if (_canvasBuffer is not null && Pyxel.Btnp(Key.V))
            {
                _host.AddPreHistory();
                var width = _canvasBuffer.GetLength(1);
                var height = _canvasBuffer.GetLength(0);
                width -= Math.Max(_selectX1 + width - 16, 0);
                height -= Math.Max(_selectY1 + height - 16, 0);
                var clipped = new TValue[height, width];
                for (var yi = 0; yi < height; yi++)
                {
                    for (var xi = 0; xi < width; xi++)
                    {
                        clipped[yi, xi] = _canvasBuffer[yi, xi];
                    }
                }
                Canvas.SetSlice(FocusX * 8 + _selectX1, FocusY * 8 + _selectY1, clipped);
                _host.AddPostHistory();
            }
        }

        // Selection tool operations (no Ctrl/Cmd)
        if (Tool == EditorSettings.ToolSelect && !hasCmdOrCtrl)
        {
            // H: Flip horizontal
            if (Pyxel.Btnp(Key.H))
            {
                var (x, y, w, h) = SelectionRect();
                _host.AddPreHistory();
                Canvas.Blt(x, y, Canvas, x, y, -w, h);
                _host.AddPostHistory();
            }

            // V: Flip vertical
            if (Pyxel.Btnp(Key.V))
            {
                var (x, y, w, h) = SelectionRect();
                _host.AddPreHistory();
                Canvas.Blt(x, y, Canvas, x, y, w, -h);
                _host.AddPostHistory();
            }
        }

        // Move tile focus (tilemap mode: Shift+arrows)
        _host.UpdateTileFocus();

        // Move target focus (only when no modifiers held)
        if (!EditorSettings.IsModifierPressed())
        {
            if (Pyxel.Btnp(Key.Left, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
            {
                _host.FocusXVar.Set(FocusX - 1);
            }
            if (Pyxel.Btnp(Key.Right, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
            {
                _host.FocusXVar.Set(FocusX + 1);
            }
            if (Pyxel.Btnp(Key.Up, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
            {
                _host.FocusYVar.Set(FocusY - 1);
            }
            if (Pyxel.Btnp(Key.Down, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
            {
                _host.FocusYVar.Set(FocusY + 1);
            }
        }
    }

    private void OnDraw()
    {
        DrawPanel(X, Y, Width, Height);

        // Draw edit panel
        var (canvas, offsetX, offsetY) = _isDragged
            ? (_editCanvas, 0, 0)
            : (Canvas, FocusX * 8, FocusY * 8);

        PyxelUser.UserPal();
        _host.DrawCanvas(X, Y, canvas, offsetX, offsetY);
        Pyxel.Pal();

        Pyxel.Line(X + 1, Y + 64, X + 128, Y + 64, WidgetSettings.PanelColor);
        Pyxel.Line(X + 64, Y + 1, X + 64, Y + 128, WidgetSettings.PanelColor);

        // Draw selection area
        if (Tool == EditorSettings.ToolSelect)
        {
            var x = X + 1 + _selectX1 * 8;
            var y = Y + 1 + _selectY1 * 8;
            var w = (_selectX2 - _selectX1 + 1) * 8;
            var h = (_selectY2 - _selectY1 + 1) * 8;
            Pyxel.Clip(X + 1, Y + 1, 128, 128);
            Pyxel.Rectb(x, y, w, h, EditorSettings.PanelSelectFrameColor);
            Pyxel.Rectb(x + 1, y + 1, w - 2, h - 2, EditorSettings.PanelSelectBorderColor);
            Pyxel.Rectb(x - 1, y - 1, w + 2, h + 2, EditorSettings.PanelSelectBorderColor);
            Pyxel.Clip();
        }
    }
}
