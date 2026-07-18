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

public sealed class ImageEditor : EditorBase, ICanvasHost
{
    private readonly ColorPicker _colorPicker;
    private readonly RadioButton _toolButton;
    private readonly NumberPicker _imagePicker;
    private readonly ImageViewer _imageViewer;
    private readonly CanvasPanel _canvasPanel;

    public WidgetVar<int> ColorVar { get; }
    public WidgetVar<int> ToolVar { get; }
    public WidgetVar<int> ImageIndexVar { get; }
    public WidgetVar<Image> CanvasVar { get; }
    public WidgetVar<int> FocusXVar { get; }
    public WidgetVar<int> FocusYVar { get; }

    public ImageEditor(App parent)
        : base(parent)
    {
        CanvasVar = new WidgetVar<Image>(null!);
        CanvasVar.AddGetFilter(_ => Pyxel.Images[ImageIndexVar!.Get()]);

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

        _canvasPanel = new CanvasPanel(this);

        // Set event listeners
        UndoPerformed += data => RestoreState((ImageEditHistory)data, old: true);
        RedoPerformed += data => RestoreState((ImageEditHistory)data, old: false);
        Dropped += OnDrop;
        Update += OnUpdate;
        Draw += OnDraw;
    }

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
            CanvasVar.Get().SetSlice(FocusXVar.Get() * 8, FocusYVar.Get() * 8,
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
