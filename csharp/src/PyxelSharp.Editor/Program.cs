// pyxel edit equivalent: pyxel-edit [PYXEL_RESOURCE_FILE] [STARTING_EDITOR]
using PyxelSharp.Editor;

var resourceFile = args.Length > 0 ? args[0] : "my_resource";
var startingEditor = args.Length > 1 ? args[1] : "image";

// cli.py _complete_extension: append .pyxres when no extension is given
if (!resourceFile.EndsWith(EditorSettings.ResourceFileExtension, StringComparison.OrdinalIgnoreCase))
{
    resourceFile += EditorSettings.ResourceFileExtension;
}

new App(resourceFile, startingEditor).Run();
