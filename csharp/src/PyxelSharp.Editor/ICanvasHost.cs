using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// Python uses duck typing (hasattr) to share vars between an editor and its
// CanvasPanel / ImageViewer; these interfaces make that contract explicit.

/// <summary>Vars an editor must expose to host a <see cref="CanvasPanel"/> /
/// <see cref="ImageViewer"/> (image mode).</summary>
internal interface ICanvasHost
{
    WidgetVar<int> ColorVar { get; }
    WidgetVar<int> ToolVar { get; }
    WidgetVar<int> ImageIndexVar { get; }
    WidgetVar<Image> CanvasVar { get; }
    WidgetVar<int> FocusXVar { get; }
    WidgetVar<int> FocusYVar { get; }
    void AddHistory(object data);
}

/// <summary>Marker for tilemap mode (Python: <c>hasattr(parent,
/// "tilemap_index_var")</c>). Implemented by TilemapEditor in slice 2.</summary>
internal interface ITilemapHost
{
    WidgetVar<int> TilemapIndexVar { get; }
}
