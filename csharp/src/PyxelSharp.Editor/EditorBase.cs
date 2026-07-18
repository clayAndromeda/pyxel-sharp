using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// editor_base.py
public abstract class EditorBase : Widget
{
    private static readonly Dictionary<int, string> ToolHelp = new()
    {
        [EditorSettings.ToolSelect] = "SELECT:S",
        [EditorSettings.ToolPencil] = "PENCIL:P",
        [EditorSettings.ToolRectb] = "RECTANGLE:R",
        [EditorSettings.ToolRect] = "FILLED-RECT:SHIFT+R",
        [EditorSettings.ToolCircb] = "CIRCLE:C",
        [EditorSettings.ToolCirc] = "FILLED-CIRC:SHIFT+C",
        [EditorSettings.ToolBucket] = "BUCKET:B",
    };

    private readonly List<object> _historyList = [];
    private int _historyIndex;

    public WidgetVar<string> HelpMessageVar { get; }

    public event Action<object>? UndoPerformed;
    public event Action<object>? RedoPerformed;
    public event Action<string>? Dropped;

    protected EditorBase(App parent)
        : base(parent, 0, 0, 0, 0, isVisible: false)
    {
        HelpMessageVar = parent.HelpMessageVar;
    }

    public string HelpMessage
    {
        get => HelpMessageVar.Get();
        set => HelpMessageVar.Set(value);
    }

    // Public methods

    public bool CanUndo => _historyIndex > 0;

    public bool CanRedo => _historyIndex < _historyList.Count;

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        _historyIndex--;
        UndoPerformed?.Invoke(_historyList[_historyIndex]);
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        RedoPerformed?.Invoke(_historyList[_historyIndex]);
        _historyIndex++;
    }

    public void AddHistory(object data)
    {
        _historyList.RemoveRange(_historyIndex, _historyList.Count - _historyIndex);
        _historyList.Add(data);
        _historyIndex++;
    }

    public void ResetHistory()
    {
        _historyList.Clear();
        _historyIndex = 0;
    }

    public void NotifyDrop(string filename) => Dropped?.Invoke(filename);

    public void AddNumberPickerHelp(NumberPicker numberPicker)
    {
        numberPicker.DecButton.MouseHover += (_, _) => HelpMessage = "-10:SHIFT+CLICK";
        numberPicker.IncButton.MouseHover += (_, _) => HelpMessage = "+10:SHIFT+CLICK";
    }

    protected void CheckToolButtonShortcuts(WidgetVar<int> toolVar)
    {
        if (Pyxel.Btn(Key.Ctrl) || Pyxel.Btn(Key.Alt) || Pyxel.Btn(Key.Gui))
        {
            return;
        }

        if (Pyxel.Btnp(Key.S))
        {
            toolVar.Set(EditorSettings.ToolSelect);
        }
        else if (Pyxel.Btnp(Key.P))
        {
            toolVar.Set(EditorSettings.ToolPencil);
        }
        else if (Pyxel.Btnp(Key.R))
        {
            toolVar.Set(Pyxel.Btn(Key.Shift) ? EditorSettings.ToolRect : EditorSettings.ToolRectb);
        }
        else if (Pyxel.Btnp(Key.C))
        {
            toolVar.Set(Pyxel.Btn(Key.Shift) ? EditorSettings.ToolCirc : EditorSettings.ToolCircb);
        }
        else if (Pyxel.Btnp(Key.B))
        {
            toolVar.Set(EditorSettings.ToolBucket);
        }
    }

    protected void AddToolButtonHelp(RadioButton toolButton)
    {
        toolButton.MouseHover += (x, y) =>
            HelpMessage = toolButton.CheckValue(x, y) is { } value
                && ToolHelp.TryGetValue(value, out var help) ? help : "";
    }
}
