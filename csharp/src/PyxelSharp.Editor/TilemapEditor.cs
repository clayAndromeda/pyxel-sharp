using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// tilemap_editor.py
//
// Undo history entry (Python dict): whole-bank snapshots carry Old/NewData +
// Old/NewImageSource, focus-area edits carry FocusPos + Old/NewCanvas.
internal sealed class TilemapEditHistory
{
    public int TilemapIndex;
    public int OldImageSource;
    public int NewImageSource;
    public (int X, int Y) FocusPos;
    public Tile[,]? OldData;
    public Tile[,]? NewData;
    public Tile[,]? OldCanvas;
    public Tile[,]? NewCanvas;
}

public sealed class TilemapEditor : EditorBase,
    ICanvasPanelHost<Tile>, IImageViewerHost, ITilemapHost
{
    // Sentinel tile that marks cells touched by tilemap-mode drawing primitives.
    private static readonly Tile EmptyTile = new(255, 255);

    private readonly RadioButton _toolButton;
    private readonly NumberPicker _tilemapPicker;
    private readonly TilemapViewer _tilemapViewer;
    private readonly NumberPicker _imagePicker;
    private readonly ImageViewer _imageViewer;
    private readonly CanvasPanel<Tile> _canvasPanel;

    private TilemapEditHistory? _historyData;
    private (Tile[,] Data, int ImageSource)? _bankBuffer;

    public WidgetVar<int> ToolVar { get; }
    public WidgetVar<int> TilemapIndexVar { get; }
    public WidgetVar<int> ImageIndexVar { get; }
    public WidgetVar<int> FocusXVar { get; }
    public WidgetVar<int> FocusYVar { get; }
    public WidgetVar<int> TileXVar { get; }
    public WidgetVar<int> TileYVar { get; }
    public WidgetVar<int> TileWVar { get; }
    public WidgetVar<int> TileHVar { get; }

    public TilemapEditor(App parent)
        : base(parent)
    {
        _toolButton = new RadioButton(this, 81, 161, EditorSettings.EditorImage,
            63, 0, numButtons: 7, value: EditorSettings.ToolPencil);
        AddToolButtonHelp(_toolButton);
        ToolVar = _toolButton.ValueVar;

        _tilemapPicker = new NumberPicker(this, 48, 161, 0, Pyxel.NumTilemaps - 1, 0);
        _tilemapPicker.Change += OnTilemapPickerChange;
        _tilemapPicker.MouseHover += (_, _) => HelpMessage = "COPY_ALL:CTRL+SHIFT+C/X/V";
        AddNumberPickerHelp(_tilemapPicker);
        TilemapIndexVar = _tilemapPicker.ValueVar;

        _tilemapViewer = new TilemapViewer(this);
        FocusXVar = _tilemapViewer.FocusXVar;
        FocusYVar = _tilemapViewer.FocusYVar;

        _imagePicker = new NumberPicker(this, 192, 161, 0, Pyxel.NumImages - 1,
            Pyxel.Tilemaps[TilemapIndexVar.Get()].ImageSourceIndex ?? 0);
        _imagePicker.Change += OnImagePickerChange;
        AddNumberPickerHelp(_imagePicker);
        ImageIndexVar = _imagePicker.ValueVar;

        _imageViewer = new ImageViewer(this);
        TileXVar = _imageViewer.FocusXVar;
        TileYVar = _imageViewer.FocusYVar;
        TileWVar = _imageViewer.FocusWVar;
        TileHVar = _imageViewer.FocusHVar;

        _canvasPanel = new CanvasPanel<Tile>(this);

        // Set event listeners
        UndoPerformed += data => RestoreState((TilemapEditHistory)data, old: true);
        RedoPerformed += data => RestoreState((TilemapEditHistory)data, old: false);
        Dropped += OnDrop;
        Update += () => CheckToolButtonShortcuts(ToolVar);
        Draw += OnDraw;
    }

    // ICanvasPanelHost<Tile> (canvas_panel.py tilemap-mode specifics)

    ICanvas<Tile> ICanvasPanelHost<Tile>.Canvas => Canvas;
    private TilemapCanvas Canvas => new(Pyxel.Tilemaps[TilemapIndexVar.Get()]);

    ICanvas<Tile> ICanvasPanelHost<Tile>.CreateEditCanvas() =>
        new TilemapCanvas(new Tilemap(16, 16, 0));

    Tile ICanvasPanelHost<Tile>.DrawValue => EmptyTile;

    Tile ICanvasPanelHost<Tile>.EraseValue => new(0, 0);

    void ICanvasPanelHost<Tile>.PickValue(Tile value)
    {
        TileXVar.Set(value.X);
        TileYVar.Set(value.Y);
    }

    void ICanvasPanelHost<Tile>.FinishEditCanvas(ICanvas<Tile> editCanvas, int pressX, int pressY)
    {
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                if (editCanvas.Pget(x, y) != EmptyTile)
                {
                    continue;
                }
                editCanvas.Pset(x, y, new Tile(
                    TileXVar.Get() + EditorMath.Mod(x - pressX, TileWVar.Get()),
                    TileYVar.Get() + EditorMath.Mod(y - pressY, TileHVar.Get())));
            }
        }
    }

    public void AddPreHistory(bool bankCopy = false)
    {
        var data = new TilemapEditHistory { TilemapIndex = TilemapIndexVar.Get() };
        _historyData = data;

        if (bankCopy)
        {
            data.OldImageSource = Canvas.Tilemap.ImageSourceIndex ?? 0;
            data.OldData = Canvas.GetSlice(0, 0, 256, 256);
        }
        else
        {
            data.FocusPos = (FocusXVar.Get(), FocusYVar.Get());
            data.OldCanvas = Canvas.GetSlice(FocusXVar.Get() * 8, FocusYVar.Get() * 8, 16, 16);
        }
    }

    public void AddPostHistory(bool bankCopy = false)
    {
        var data = _historyData!;

        if (bankCopy)
        {
            data.NewData = Canvas.GetSlice(0, 0, 256, 256);
            data.NewImageSource = Canvas.Tilemap.ImageSourceIndex ?? 0;
            if (!CanvasData.SliceEquals(data.NewData, data.OldData!)
                || data.NewImageSource != data.OldImageSource)
            {
                AddHistory(data);
            }
        }
        else
        {
            data.NewCanvas = Canvas.GetSlice(FocusXVar.Get() * 8, FocusYVar.Get() * 8, 16, 16);
            if (!CanvasData.SliceEquals(data.NewCanvas, data.OldCanvas!))
            {
                AddHistory(data);
            }
        }
    }

    public void BankClipboardCopy()
    {
        var tilemap = Pyxel.Tilemaps[TilemapIndexVar.Get()];
        _bankBuffer = (tilemap.GetSlice(0, 0, 256, 256), tilemap.ImageSourceIndex ?? 0);
    }

    public void BankClipboardCut()
    {
        AddPreHistory(bankCopy: true);
        Pyxel.Tilemaps[TilemapIndexVar.Get()].Rect(0, 0, 256, 256, new Tile(0, 0));
        AddPostHistory(bankCopy: true);
    }

    public void BankClipboardPaste()
    {
        if (_bankBuffer is not { } buffer)
        {
            return;
        }
        AddPreHistory(bankCopy: true);
        Pyxel.Tilemaps[TilemapIndexVar.Get()].SetSlice(0, 0, buffer.Data);
        ImageIndexVar.Set(buffer.ImageSource);
        AddPostHistory(bankCopy: true);
    }

    void ICanvasPanelHost<Tile>.UpdateTileFocus()
    {
        if (!Pyxel.Btn(Key.Shift))
        {
            return;
        }
        if (Pyxel.Btnp(Key.Left, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            TileXVar.Set(TileXVar.Get() - 1);
        }
        if (Pyxel.Btnp(Key.Right, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            TileXVar.Set(TileXVar.Get() + 1);
        }
        if (Pyxel.Btnp(Key.Up, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            TileYVar.Set(TileYVar.Get() - 1);
        }
        if (Pyxel.Btnp(Key.Down, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            TileYVar.Set(TileYVar.Get() + 1);
        }
    }

    void ICanvasPanelHost<Tile>.DrawCanvas(
        int panelX, int panelY, ICanvas<Tile> canvas, int offsetX, int offsetY) =>
        Pyxel.Bltm(panelX + 1, panelY + 1, ((TilemapCanvas)canvas).Tilemap,
            offsetX * 8, offsetY * 8, 128, 128);

    // Helpers

    private void RestoreState(TilemapEditHistory data, bool old)
    {
        TilemapIndexVar.Set(data.TilemapIndex);
        var bankData = old ? data.OldData : data.NewData;
        if (bankData is not null)
        {
            Pyxel.Tilemaps[TilemapIndexVar.Get()].SetSlice(0, 0, bankData);
            ImageIndexVar.Set(old ? data.OldImageSource : data.NewImageSource);
        }
        else
        {
            FocusXVar.Set(data.FocusPos.X);
            FocusYVar.Set(data.FocusPos.Y);
            Pyxel.Tilemaps[TilemapIndexVar.Get()].SetSlice(
                FocusXVar.Get() * 8, FocusYVar.Get() * 8,
                (old ? data.OldCanvas : data.NewCanvas)!);
        }
    }

    // Event handlers

    private void OnTilemapPickerChange(int value) =>
        ImageIndexVar.Set(Pyxel.Tilemaps[value].ImageSourceIndex ?? 0);

    private void OnImagePickerChange(int value) =>
        Pyxel.Tilemaps[TilemapIndexVar.Get()].SetImageSource(value);

    private void OnDrop(string filename)
    {
        try
        {
            Pyxel.Tilemaps[TilemapIndexVar.Get()].Load(
                FocusXVar.Get() * 8, FocusYVar.Get() * 8, filename, 0);
        }
        catch (PyxelException e)
        {
            Console.WriteLine($"Failed to load tilemap: {e.Message}");
        }
    }

    private void OnDraw()
    {
        DrawPanel(11, 156, 136, 17);
        DrawPanel(157, 156, 72, 17);
        Pyxel.Text(18, 162, "TILEMAP", EditorSettings.TextLabelColor);
        Pyxel.Text(170, 162, "IMAGE", EditorSettings.TextLabelColor);
    }
}
