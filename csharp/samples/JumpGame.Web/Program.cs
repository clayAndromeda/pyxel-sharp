using System.Runtime.InteropServices.JavaScript;

// On browser-wasm the game is started from JS via GameEntry.Start, not from
// Main: emscripten_set_main_loop unwinds the stack with a JS exception, and
// going through dotnet's Main invocation would tear the main loop down.
Console.WriteLine("JumpGame.Web: runtime started");

public static partial class GameEntry
{
    [JSExport]
    public static void Start()
    {
        // Preloaded into MEMFS by dotnet.js (WasmFilesToIncludeInFileSystem).
        // Never returns on emscripten: the ctor calls Pyxel.Run and the call
        // unwinds back to the JS caller; the App instance stays alive through
        // the update/draw delegates Pyxel.Run stores.
        _ = new App("/assets/jump_game.pyxres");
    }
}
