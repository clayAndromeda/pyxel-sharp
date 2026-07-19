namespace PyxelSharp.Editor;

// Python's field_cursor mutates live lists (sound.notes etc.). PyxelSharp's
// array properties are copy-swap, so this view gives list semantics by
// reading the array, mutating a copy, and writing it back on every operation.
public sealed class FieldView(Func<int[]> get, Action<int[]> set)
{
    public int Count => get().Length;

    public int this[int index]
    {
        get => get()[index];
        set
        {
            var values = get();
            values[index] = value;
            set(values);
        }
    }

    public int[] ToArray() => get();

    public void ReplaceAll(int[] values) => set((int[])values.Clone());

    public void Clear() => set([]);

    /// <summary>Python: <c>del field[start : start + count]</c> (clamped).</summary>
    public void RemoveRange(int start, int count)
    {
        var values = get().ToList();
        start = Math.Clamp(start, 0, values.Count);
        count = Math.Clamp(count, 0, values.Count - start);
        values.RemoveRange(start, count);
        set([.. values]);
    }

    /// <summary>Python: <c>field[start:start] = items</c> (clamped).</summary>
    public void InsertRange(int start, int[] items)
    {
        var values = get().ToList();
        start = Math.Clamp(start, 0, values.Count);
        values.InsertRange(start, items);
        set([.. values]);
    }

    /// <summary>Python: <c>del field[maxLength:]</c>.</summary>
    public void Truncate(int maxLength)
    {
        var values = get();
        if (values.Length > maxLength)
        {
            set(values[..maxLength]);
        }
    }

    public void Append(int value)
    {
        var values = get();
        set([.. values, value]);
    }
}
