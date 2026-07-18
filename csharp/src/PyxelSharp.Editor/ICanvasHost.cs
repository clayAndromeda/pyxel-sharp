using PyxelSharp.Editor.Widgets;

namespace PyxelSharp.Editor;

// Python uses duck typing (hasattr) to share vars between an editor and its
// panels; these interfaces make those contracts explicit.

/// <summary>What an editor must expose to host a <see cref="CanvasPanel{TValue}"/>.
/// Mode-specific behavior (history capture, bank clipboard, tile stamping,
/// canvas blitting) lives on the editor so the panel stays generic.</summary>
internal interface ICanvasPanelHost<TValue>
{
    WidgetVar<int> ToolVar { get; }
    WidgetVar<int> FocusXVar { get; }
    WidgetVar<int> FocusYVar { get; }

    /// <summary>The current bank as a canvas (Python: <c>canvas_var</c>).</summary>
    ICanvas<TValue> Canvas { get; }

    /// <summary>Creates the 16x16 scratch canvas (Python: <c>_edit_canvas</c>).</summary>
    ICanvas<TValue> CreateEditCanvas();

    /// <summary>The value drawing primitives write (image: current color;
    /// tilemap: the EMPTY_TILE sentinel).</summary>
    TValue DrawValue { get; }

    /// <summary>The value used to clear cut regions (image: 0; tilemap: (0,0)).</summary>
    TValue EraseValue { get; }

    /// <summary>Right-click pick (image: color; tilemap: tile).</summary>
    void PickValue(TValue value);

    /// <summary>Post-processes the scratch canvas after a drawing primitive
    /// (tilemap: expands EMPTY_TILE cells into the selected tile stamp).</summary>
    void FinishEditCanvas(ICanvas<TValue> editCanvas, int pressX, int pressY);

    void AddPreHistory(bool bankCopy = false);
    void AddPostHistory(bool bankCopy = false);

    void BankClipboardCopy();
    void BankClipboardCut();
    void BankClipboardPaste();

    /// <summary>Per-frame extra key handling (tilemap: Shift+arrows move the
    /// tile stamp focus).</summary>
    void UpdateTileFocus();

    /// <summary>Draws the canvas region into the panel (image: blt with x8
    /// scale; tilemap: bltm). Called with user_pal applied.</summary>
    void DrawCanvas(int panelX, int panelY, ICanvas<TValue> canvas, int offsetX, int offsetY);
}

/// <summary>Vars an editor must expose to host an <see cref="ImageViewer"/>.</summary>
internal interface IImageViewerHost
{
    WidgetVar<int> ImageIndexVar { get; }
}

/// <summary>Marker for tilemap mode (Python: <c>hasattr(parent,
/// "tilemap_index_var")</c>).</summary>
internal interface ITilemapHost
{
    WidgetVar<int> TilemapIndexVar { get; }
}
