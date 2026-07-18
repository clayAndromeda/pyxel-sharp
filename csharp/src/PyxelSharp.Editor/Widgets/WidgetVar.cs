namespace PyxelSharp.Editor.Widgets;

// widgets/widget_var.py
//
// A shared observable value. Python binds these as dynamic module attributes
// (new_var/copy_var); in C# each widget exposes them as typed properties and
// "copy_var" is sharing the same WidgetVar<T> instance.
//
// Get filters transform the value on read (e.g. visibility ANDed with the
// parent's), set filters transform it on write (e.g. clamping); Changed fires
// only when the stored value actually changes.
public sealed class WidgetVar<T>(T value)
{
    private T _value = value;
    private readonly List<Func<T, T>> _getFilters = [];
    private readonly List<Func<T, T>> _setFilters = [];

    public event Action<T>? Changed;

    public T Get()
    {
        var result = _value;
        foreach (var filter in _getFilters)
        {
            result = filter(result);
        }
        return result;
    }

    public void Set(T value)
    {
        foreach (var filter in _setFilters)
        {
            value = filter(value);
        }

        if (EqualityComparer<T>.Default.Equals(_value, value))
        {
            return;
        }

        _value = value;
        Changed?.Invoke(value);
    }

    public void AddGetFilter(Func<T, T> filter) => _getFilters.Add(filter);
    public void RemoveGetFilter(Func<T, T> filter) => _getFilters.Remove(filter);
    public void AddSetFilter(Func<T, T> filter) => _setFilters.Add(filter);
    public void RemoveSetFilter(Func<T, T> filter) => _setFilters.Remove(filter);
}
