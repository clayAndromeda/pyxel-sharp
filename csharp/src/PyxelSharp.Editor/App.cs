using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// app.py
public sealed class App : Widget
{
    private static readonly string[] EditorTypes = ["image", "tilemap", "sound", "music"];

    private readonly string _resourceFile;
    private readonly RadioButton _editorButton;
    private readonly ImageButton _undoButton;
    private readonly ImageButton _redoButton;
    private readonly ImageButton _saveButton;
    private readonly EditorBase[] _editors;

    public WidgetVar<string> HelpMessageVar { get; }
    public WidgetVar<int> EditorTypeVar { get; }

    // headless is a test hook (EditorSmoke); the upstream app has no such flag.
    public App(string resourceFile, string startingEditor = "image", bool headless = false)
        : base(null, 0, 0, 0, 0)
    {
        // Resolve the absolute path before pyxel.init changes the working directory.
        var originalResourceFile = resourceFile;
        var resourcePath = Path.GetFullPath(resourceFile);

        if (Directory.Exists(resourcePath))
        {
            Console.WriteLine($"A directory named '{originalResourceFile}' exists");
            Environment.Exit(1);
        }
        if (!Directory.Exists(Path.GetDirectoryName(resourcePath)))
        {
            Console.WriteLine($"Directory for '{originalResourceFile}' does not exist");
            Environment.Exit(1);
        }

        Pyxel.Init(EditorSettings.AppWidth, EditorSettings.AppHeight, quitKey: Key.None,
            headless: headless ? true : null);
        Pyxel.Mouse(true);
        // Load the UI sprite sheet while the palette is still the 16 system
        // colors (Python does this at module import time).
        _ = EditorSettings.EditorImage;
        var colors = Pyxel.Colors.ToArray();
        SetTitle(originalResourceFile);

        resourceFile = resourcePath;
        if (File.Exists(resourcePath))
        {
            Pyxel.Load(resourceFile);
        }
        else
        {
            Pyxel.LoadPal(resourceFile);
        }

        // Concatenate system and user palettes so colors[:NUM_COLORS] are the
        // system set and colors[NUM_COLORS:] are the user set.
        PyxelUser.NumUserColors = Pyxel.Colors.Count;
        var combined = new uint[colors.Length + PyxelUser.NumUserColors];
        colors.CopyTo(combined, 0);
        Pyxel.Colors.ToArray().CopyTo(combined, colors.Length);
        Pyxel.Colors.Replace(combined);

        SetSize(Pyxel.Width, Pyxel.Height);
        _resourceFile = resourceFile;

        HelpMessageVar = new WidgetVar<string>("");

        _editorButton = new RadioButton(this, 1, 1, EditorSettings.EditorImage,
            0, 0, numButtons: 4,
            value: Math.Max(Array.IndexOf(EditorTypes, startingEditor), 0));
        _editorButton.Change += OnEditorButtonChange;
        _editorButton.MouseHover += (_, _) => HelpMessage = "EDITOR:ALT+LEFT/RIGHT";
        EditorTypeVar = _editorButton.ValueVar;

        _undoButton = new ImageButton(this, 48, 1, EditorSettings.EditorImage, 36, 0);
        _undoButton.Press += () => Editor.Undo();
        _undoButton.MouseHover += (_, _) => HelpMessage = "UNDO:CTRL+Z";

        _redoButton = new ImageButton(this, 57, 1, EditorSettings.EditorImage, 45, 0);
        _redoButton.Press += () => Editor.Redo();
        _redoButton.MouseHover += (_, _) => HelpMessage = "REDO:CTRL+Y";

        _saveButton = new ImageButton(this, 75, 1, EditorSettings.EditorImage, 54, 0);
        _saveButton.Press += () => Pyxel.Save(_resourceFile);
        _saveButton.MouseHover += (_, _) => HelpMessage = "SAVE:CTRL+S";

        _editors =
        [
            new ImageEditor(this),
            new PlaceholderEditor(this, "TILEMAP"),
            new PlaceholderEditor(this, "SOUND"),
            new PlaceholderEditor(this, "MUSIC"),
        ];
        OnEditorButtonChange(EditorTypeVar.Get());

        // Set event listeners
        Update += OnUpdate;
        Draw += OnDraw;
    }

    public void Run() => Pyxel.Run(UpdateAll, DrawAll);

    public IReadOnlyList<EditorBase> Editors => _editors;

    // Helpers

    private EditorBase Editor => _editors[EditorTypeVar.Get()];

    private string HelpMessage
    {
        get => HelpMessageVar.Get();
        set => HelpMessageVar.Set(value);
    }

    private static void SetTitle(string resourceFile) =>
        Pyxel.Title($"Pyxel Editor - {resourceFile}");

    // Event handlers

    private void OnEditorButtonChange(int value)
    {
        for (var i = 0; i < _editors.Length; i++)
        {
            _editors[i].IsVisible = i == value;
        }
    }

    private void OnUpdate()
    {
        var droppedFiles = Pyxel.DroppedFiles;
        if (droppedFiles.Length > 0)
        {
            var droppedFile = droppedFiles[^1];
            var fileExt = Path.GetExtension(droppedFile);
            if (fileExt == EditorSettings.ResourceFileExtension)
            {
                Pyxel.Stop();
                foreach (var editor in _editors)
                {
                    editor.ResetHistory();
                }
                Pyxel.Load(droppedFile);
                SetTitle(droppedFile);
            }
            else
            {
                Editor.NotifyDrop(droppedFile);
            }
        }

        if (Pyxel.Btn(Key.Alt))
        {
            var count = _editors.Length;
            if (Pyxel.Btnp(Key.Left))
            {
                EditorTypeVar.Set((EditorTypeVar.Get() - 1 + count) % count);
            }
            else if (Pyxel.Btnp(Key.Right))
            {
                EditorTypeVar.Set((EditorTypeVar.Get() + 1) % count);
            }
        }

        _undoButton.IsEnabled = Editor.CanUndo;
        _redoButton.IsEnabled = Editor.CanRedo;

        if (Pyxel.Btn(Key.Ctrl) || Pyxel.Btn(Key.Gui))
        {
            if (Pyxel.Btnp(Key.S))
            {
                _saveButton.IsPressed = true;
            }
            if (Editor.CanUndo && Pyxel.Btnp(Key.Z,
                hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
            {
                _undoButton.IsPressed = true;
            }
            else if (Editor.CanRedo && Pyxel.Btnp(Key.Y,
                hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
            {
                _redoButton.IsPressed = true;
            }
        }

        // Hidden save shortcut for Pyxel Code Maker.
        if (Pyxel.Btn(Key.F13))
        {
            _saveButton.IsPressed = true;
        }
    }

    private void OnDraw()
    {
        Pyxel.Cls(WidgetSettings.BackgroundColor);
        Pyxel.Rect(0, 0, 240, 9, WidgetSettings.PanelColor);
        Pyxel.Line(0, 9, 239, 9, WidgetSettings.ShadowColor);
        Pyxel.Text(93, 2, HelpMessage, EditorSettings.HelpMessageColor);
        HelpMessage = "";
    }
}
