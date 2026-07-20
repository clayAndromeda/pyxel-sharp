using System.IO;
using System.Runtime.InteropServices.JavaScript;

// Compiled into each PyxelSharp web game via PyxelSharp.Web.props.
// JS-callable helpers used by pyxel-boot.js: assets are fetched by JS and
// written into the emscripten MEMFS here, so Pyxel.Load / new Image(path)
// can read them (dotnet's System.IO and pyxel-core's std::fs share the same
// emscripten FS because everything is linked into one wasm module).
public static partial class PyxelWebHost
{
    [JSExport]
    public static void WriteFile(string path, byte[] bytes)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllBytes(path, bytes);
    }
}
