namespace PyxelSharp.Editor;

// Stand-in for editors not yet ported (tilemap: slice 2, sound: slice 3,
// music: slice 4). Not part of the upstream Python editor.
public sealed class PlaceholderEditor : EditorBase
{
    public PlaceholderEditor(App parent, string name)
        : base(parent)
    {
        Draw += () => Pyxel.Text(11, 20, $"{name} EDITOR: NOT PORTED YET",
            EditorSettings.HelpMessageColor);
    }
}
