using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// field_cursor.py
public sealed class FieldCursor
{
    private readonly int _maxFieldLength;
    // Uses an impossible wrap length to make vertical moves clamp on one row.
    private readonly int _fieldWrapLength;
    private readonly int[] _maxFieldValues;
    private readonly Func<int, FieldView?> _getField;
    private readonly Action<int?, int?, bool> _addPreHistory;
    private readonly Action<int?, int?, bool> _addPostHistory;
    private readonly bool _enableCrossFieldCopy;
    // Python checks hasattr(parent, "speed_var") lazily: the sound editor
    // attaches its speed var via SetSpeedVar once the picker exists, the
    // music editor never does.
    private WidgetVar<int>? _speedVar;

    private int _cursorX;
    private int _cursorY;
    private int? _selectX;
    private (int Y, int[] Values)? _fieldBuffer;
    private (int? Speed, int[][] Fields)? _bankBuffer;

    public FieldCursor(
        int maxFieldLength,
        int fieldWrapLength,
        int[] maxFieldValues,
        Func<int, FieldView?> getField,
        Action<int?, int?, bool> addPreHistory,
        Action<int?, int?, bool> addPostHistory,
        bool enableCrossFieldCopy)
    {
        _maxFieldLength = maxFieldLength;
        _fieldWrapLength = fieldWrapLength < maxFieldLength
            ? fieldWrapLength
            : maxFieldLength + 1;
        _maxFieldValues = maxFieldValues;
        _getField = getField;
        _addPreHistory = addPreHistory;
        _addPostHistory = addPostHistory;
        _enableCrossFieldCopy = enableCrossFieldCopy;
    }

    public void SetSpeedVar(WidgetVar<int> speedVar) => _speedVar = speedVar;

    // Properties

    public int X => IsSelecting
        ? Math.Min(AdjustedCursorX, AdjustedSelectX)
        : AdjustedCursorX;

    public int Y => _cursorY;

    public int Width
    {
        get
        {
            if (!IsSelecting)
            {
                return 1;
            }
            var width = Math.Abs(AdjustedCursorX - AdjustedSelectX) + 1;
            return Math.Min(width, Field.Count - X);
        }
    }

    public FieldView Field => _getField(_cursorY)!;

    public bool IsSelecting => _selectX is not null && Field.Count > 0;

    private int MaxCursorX => Math.Clamp(Field.Count, 0, _maxFieldLength - 1);

    private int MaxSelectX => Math.Clamp(Field.Count - 1, 0, _maxFieldLength - 1);

    private int MaxY => _maxFieldValues.Length - 1;

    private int AdjustedCursorX => Math.Min(_cursorX, MaxCursorX);

    private int AdjustedSelectX => Math.Min(_selectX!.Value, MaxSelectX);

    // Movement

    public void MoveTo(int x, int y, bool withSelectKey)
    {
        y = Math.Clamp(y, 0, MaxY);
        if (_cursorY != y)
        {
            _cursorY = y;
            _cursorX = Math.Clamp(x, 0, MaxCursorX);
            _selectX = null;
        }
        else if (withSelectKey)
        {
            if (IsSelecting)
            {
                _cursorX = Math.Clamp(x, 0, MaxSelectX);
            }
            else
            {
                _selectX = Math.Clamp(AdjustedCursorX, 0, MaxSelectX);
                _cursorX = Math.Clamp(x, 0, MaxSelectX);
            }
        }
        else
        {
            _cursorX = Math.Clamp(x, 0, MaxCursorX);
            _selectX = null;
        }
    }

    public void MoveLeft(bool withSelectKey)
    {
        if (withSelectKey)
        {
            if (IsSelecting)
            {
                _cursorX = Math.Max(AdjustedCursorX - 1, 0);
            }
            else if (Field.Count > 0)
            {
                _cursorX = Math.Min(AdjustedCursorX, MaxSelectX);
                _selectX = _cursorX;
            }
        }
        else
        {
            _cursorX = Math.Max(AdjustedCursorX - 1, 0);
            _selectX = null;
        }
    }

    public void MoveRight(bool withSelectKey)
    {
        if (withSelectKey)
        {
            if (IsSelecting)
            {
                _cursorX = Math.Min(AdjustedCursorX + 1, MaxSelectX);
            }
            else if (AdjustedCursorX <= MaxSelectX)
            {
                _cursorX = Math.Min(AdjustedCursorX, MaxSelectX);
                _selectX = _cursorX;
            }
        }
        else
        {
            _cursorX = Math.Min(AdjustedCursorX + 1, MaxCursorX);
            _selectX = null;
        }
    }

    public void MoveUp(bool withSelectKey)
    {
        if (AdjustedCursorX >= _fieldWrapLength)
        {
            if (!withSelectKey)
            {
                _selectX = null;
            }
            else if (!IsSelecting)
            {
                _selectX = AdjustedCursorX;
            }
            _cursorX -= _fieldWrapLength;
        }
        else if (_cursorY > 0)
        {
            _cursorY--;
            _cursorX = _fieldWrapLength * EditorMath.FloorDiv(Field.Count, _fieldWrapLength)
                + EditorMath.Mod(_cursorX, _fieldWrapLength);
            _selectX = null;
        }
    }

    public void MoveDown(bool withSelectKey)
    {
        if (EditorMath.FloorDiv(AdjustedCursorX, _fieldWrapLength)
            < EditorMath.FloorDiv(Field.Count, _fieldWrapLength))
        {
            if (!withSelectKey)
            {
                _selectX = null;
            }
            else if (!IsSelecting)
            {
                _selectX = AdjustedCursorX;
            }
            _cursorX += _fieldWrapLength;
        }
        else if (_cursorY < MaxY)
        {
            _cursorY++;
            _cursorX = EditorMath.Mod(_cursorX, _fieldWrapLength);
            _selectX = null;
        }
    }

    // Editing

    public void Insert(int value) => Insert([value]);

    public void Insert(int[] values)
    {
        _addPreHistory(X, Y, false);
        var x = X;
        if (IsSelecting)
        {
            Field.RemoveRange(x, Width);
        }
        Field.InsertRange(x, values);
        Field.Truncate(_maxFieldLength);
        MoveTo(x + values.Length, Y, false);
        _addPostHistory(X, Y, false);
    }

    public void Backspace()
    {
        if (!IsSelecting && X == 0)
        {
            return;
        }

        _addPreHistory(X, Y, false);
        int x;
        int width;
        if (IsSelecting)
        {
            x = X;
            width = Width;
        }
        else
        {
            x = X - 1;
            width = 1;
        }
        Field.RemoveRange(x, width);
        MoveTo(x, Y, false);
        _addPostHistory(X, Y, false);
    }

    public void Delete()
    {
        if (X >= Field.Count)
        {
            return;
        }

        _addPreHistory(X, Y, false);
        var x = X;
        Field.RemoveRange(x, Width);
        MoveTo(x, Y, false);
        _addPostHistory(X, Y, false);
    }

    public void SelectAll()
    {
        if (Field.Count == 0)
        {
            return;
        }
        _cursorX = 0;
        _selectX = Field.Count - 1;
    }

    public void Copy()
    {
        // Python slice semantics: field[x : x + width] clamps out-of-range.
        var values = Field.ToArray();
        var start = Math.Min(X, values.Length);
        var end = Math.Min(X + Width, values.Length);
        _fieldBuffer = (Y, values[start..end]);
    }

    public void Cut()
    {
        Copy();
        Delete();
    }

    public void Paste()
    {
        if (_fieldBuffer is not { } buffer)
        {
            return;
        }
        if (!_enableCrossFieldCopy && Y != buffer.Y)
        {
            return;
        }
        Insert(buffer.Values);
    }

    public void Shift(int offset)
    {
        _addPreHistory(X, Y, false);
        var field = Field;
        for (var i = X; i < X + Width; i++)
        {
            if (i < field.Count)
            {
                var value = field[i];
                if (value >= 0)
                {
                    field[i] = Math.Clamp(value + offset, 0, _maxFieldValues[Y]);
                }
            }
            else
            {
                field.Append(0);
            }
        }
        _addPostHistory(X, Y, false);
    }

    // Input processing

    public void ProcessInput()
    {
        if (Pyxel.Btn(Key.Alt))
        {
            return;
        }

        // Copy/cut/paste bank
        if (Pyxel.Btn(Key.Shift) && (Pyxel.Btn(Key.Ctrl) || Pyxel.Btn(Key.Gui)))
        {
            // Ctrl+Shift+C/Ctrl+Shift+X: Copy bank
            if (Pyxel.Btnp(Key.C) || Pyxel.Btnp(Key.X))
            {
                var fields = new int[MaxY + 1][];
                for (var i = 0; i <= MaxY; i++)
                {
                    fields[i] = _getField(i)!.ToArray();
                }
                _bankBuffer = (_speedVar?.Get(), fields);
            }

            // Ctrl+Shift+X: Cut bank
            if (Pyxel.Btnp(Key.X))
            {
                _addPreHistory(null, null, true);
                for (var i = 0; i <= MaxY; i++)
                {
                    _getField(i)!.Clear();
                }
                _addPostHistory(null, null, true);
            }

            // Ctrl+Shift+V: Paste bank
            if (Pyxel.Btnp(Key.V) && _bankBuffer is { } buffer)
            {
                _addPreHistory(null, null, true);
                if (_speedVar is not null && buffer.Speed is { } speed)
                {
                    _speedVar.Set(speed);
                }
                for (var i = 0; i <= MaxY; i++)
                {
                    _getField(i)!.ReplaceAll(buffer.Fields[i]);
                }
                _addPostHistory(null, null, true);
            }
            return;
        }

        // Copy/cut/paste/shift field
        if (!Pyxel.Btn(Key.Shift) && (Pyxel.Btn(Key.Ctrl) || Pyxel.Btn(Key.Gui)))
        {
            // Ctrl+A: Select all
            if (Pyxel.Btnp(Key.A))
            {
                SelectAll();
            }

            // Ctrl+C: Copy
            if (Pyxel.Btnp(Key.C))
            {
                Copy();
            }

            // Ctrl+X: Cut
            if (Pyxel.Btnp(Key.X))
            {
                Cut();
            }

            // Ctrl+V: Paste
            if (Pyxel.Btnp(Key.V))
            {
                Paste();
            }

            // Ctrl+U: Shift up
            if (Pyxel.Btnp(Key.U, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
            {
                Shift(1);
            }

            // Ctrl+D: Shift down
            if (Pyxel.Btnp(Key.D, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
            {
                Shift(-1);
            }
            return;
        }

        var withSelectKey = Pyxel.Btn(Key.Shift);
        if (Pyxel.Btnp(Key.Left, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            MoveLeft(withSelectKey);
        }
        if (Pyxel.Btnp(Key.Right, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            MoveRight(withSelectKey);
        }
        if (Pyxel.Btnp(Key.Up, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            MoveUp(withSelectKey);
        }
        if (Pyxel.Btnp(Key.Down, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            MoveDown(withSelectKey);
        }
        if (Pyxel.Btnp(Key.Backspace, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            Backspace();
        }
        if (Pyxel.Btnp(Key.Delete, hold: WidgetSettings.HoldTime, repeat: WidgetSettings.RepeatTime))
        {
            Delete();
        }
    }
}
