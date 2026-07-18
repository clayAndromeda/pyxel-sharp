using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// image_editor.py
//
// Undo history entry. Python uses a dict with optional keys; whole-bank
// snapshots use Old/NewData, focus-area edits use FocusPos + Old/NewCanvas.
internal sealed class ImageEditHistory
{
    public int ImageIndex;
    public (int X, int Y) FocusPos;
    public Color[,]? OldData;
    public Color[,]? NewData;
    public Color[,]? OldCanvas;
    public Color[,]? NewCanvas;
}

public sealed class ImageEditor : EditorBase, ICanvasPanelHost<Color>, IImageViewerHost
{
    private readonly ColorPicker _colorPicker;
    private readonly RadioButton _toolButton;
    private readonly NumberPicker _imagePicker;
    private readonly ImageViewer _imageViewer;
    private readonly CanvasPanel<Color> _canvasPanel;

    private ImageEditHistory? _historyData;
    private Color[,]? _bankBuffer;

    public WidgetVar<int> ColorVar { get; }
    public WidgetVar<int> ToolVar { get; }
    public WidgetVar<int> ImageIndexVar { get; }
    public WidgetVar<int> FocusXVar { get; }
    public WidgetVar<int> FocusYVar { get; }

    public ImageEditor(App parent)
        : base(parent)
    {
        _colorPicker = new ColorPicker(this, 11, 156,
            Math.Min(7, PyxelUser.NumUserColors - 1), withShadow: false);
        _colorPicker.MouseHover += (_, _) => HelpMessage = "COLOR:1-8/SHIFT+1-8";
        ColorVar = _colorPicker.ValueVar;

        _toolButton = new RadioButton(this, 81, 161, EditorSettings.EditorImage,
            63, 0, numButtons: 7, value: EditorSettings.ToolPencil);
        AddToolButtonHelp(_toolButton);
        ToolVar = _toolButton.ValueVar;

        _imagePicker = new NumberPicker(this, 192, 161, 0, Pyxel.NumImages - 1, 0);
        _imagePicker.MouseHover += (_, _) => HelpMessage = "COPY_ALL:CTRL+SHIFT+C/X/V";
        AddNumberPickerHelp(_imagePicker);
        ImageIndexVar = _imagePicker.ValueVar;

        _imageViewer = new ImageViewer(this);
        FocusXVar = _imageViewer.FocusXVar;
        FocusYVar = _imageViewer.FocusYVar;

        _canvasPanel = new CanvasPanel<Color>(this);

        // Set event listeners
        UndoPerformed += data => RestoreState((ImageEditHistory)data, old: true);
        RedoPerformed += data => RestoreState((ImageEditHistory)data, old: false);
        Dropped += OnDrop;
        Update += OnUpdate;
        Draw += OnDraw;
    }

    // ICanvasPanelHost<Color> (canvas_panel.py image-mode specifics)

    ICanvas<Color> ICanvasPanelHost<Color>.Canvas => Canvas;
    private ImageCanvas Canvas => new(Pyxel.Images[ImageIndexVar.Get()]);

    ICanvas<Color> ICanvasPanelHost<Color>.CreateEditCanvas() => new ImageCanvas(new Image(16, 16));

    Color ICanvasPanelHost<Color>.DrawValue => (byte)ColorVar.Get();

    Color ICanvasPanelHost<Color>.EraseValue => 0;

    void ICanvasPanelHost<Color>.PickValue(Color value) => ColorVar.Set(value.Value);

    void ICanvasPanelHost<Color>.FinishEditCanvas(ICanvas<Color> editCanvas, int pressX, int pressY)
    {
    }

    public void AddPreHistory(bool bankCopy = false)
    {
        var data = new ImageEditHistory { ImageIndex = ImageIndexVar.Get() };
        _historyData = data;

        if (bankCopy)
        {
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
            if (!CanvasData.SliceEquals(data.NewData, data.OldData!))
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

    public void BankClipboardCopy() =>
        _bankBuffer = Pyxel.Images[ImageIndexVar.Get()].GetSlice(0, 0, 256, 256);

    public void BankClipboardCut()
    {
        AddPreHistory(bankCopy: true);
        Pyxel.Images[ImageIndexVar.Get()].Rect(0, 0, 256, 256, 0);
        AddPostHistory(bankCopy: true);
    }

    public void BankClipboardPaste()
    {
        if (_bankBuffer is null)
        {
            return;
        }
        AddPreHistory(bankCopy: true);
        Pyxel.Images[ImageIndexVar.Get()].SetSlice(0, 0, _bankBuffer);
        AddPostHistory(bankCopy: true);
    }

    void ICanvasPanelHost<Color>.UpdateTileFocus()
    {
    }

    void ICanvasPanelHost<Color>.DrawCanvas(
        int panelX, int panelY, ICanvas<Color> canvas, int offsetX, int offsetY) =>
        // blt scales centered on (x + (w-1)/2, y + (h-1)/2); shift dest by
        // (w * (scale - 1) + 1) / 2 = 56.5 so the 128x128 output aligns to
        // (panelX + 1, panelY + 1). Integer 57 rounds the center identically.
        Pyxel.Blt(panelX + 57, panelY + 57, ((ImageCanvas)canvas).Image,
            offsetX, offsetY, 16, 16, scale: 8);

    // Helpers

    private void RestoreState(ImageEditHistory data, bool old)
    {
        ImageIndexVar.Set(data.ImageIndex);
        var bankData = old ? data.OldData : data.NewData;
        if (bankData is not null)
        {
            Pyxel.Images[ImageIndexVar.Get()].SetSlice(0, 0, bankData);
        }
        else
        {
            FocusXVar.Set(data.FocusPos.X);
            FocusYVar.Set(data.FocusPos.Y);
            Pyxel.Images[ImageIndexVar.Get()].SetSlice(
                FocusXVar.Get() * 8, FocusYVar.Get() * 8,
                (old ? data.OldCanvas : data.NewCanvas)!);
        }
    }

    // Event handlers

    private void OnDrop(string filename)
    {
        var colors = Pyxel.Colors.ToArray();
        var userColors = colors[Pyxel.NumColors..];
        // Load dropped images against the user sub-palette instead of system colors.
        Pyxel.Colors.Replace(userColors);
        try
        {
            Pyxel.Images[ImageIndexVar.Get()].Load(
                FocusXVar.Get() * 8, FocusYVar.Get() * 8, filename);
        }
        catch (PyxelException e)
        {
            Console.WriteLine($"Failed to load image: {e.Message}");
        }
        finally
        {
            Pyxel.Colors.Replace(colors);
        }
    }

    private void OnUpdate()
    {
        CheckToolButtonShortcuts(ToolVar);

        // Handle color shortcut keys (KEY_1..KEY_8)
        if (!Pyxel.Btn(Key.Alt))
        {
            for (var i = 0; i < 8; i++)
            {
                if (Pyxel.Btnp((Key)((int)Key.D1 + i)))
                {
                    var col = i;
                    if (Pyxel.Btn(Key.Shift))
                    {
                        col += 8;
                    }
                    ColorVar.Set(col);
                    break;
                }
            }
        }
    }

    private void OnDraw()
    {
        DrawPanel(11, 156, 136, 17);
        DrawPanel(157, 156, 72, 17);
        Pyxel.Text(170, 162, "IMAGE", EditorSettings.TextLabelColor);
    }
}
