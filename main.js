import { dotnet } from './_framework/dotnet.js'

const canvas = document.getElementById('canvas');
const status = document.getElementById('status');

const { getAssemblyExports, getConfig } = await dotnet
    .withModuleConfig({
        canvas,
        // Keep the emscripten runtime alive after Start() unwinds into the
        // browser-driven main loop.
        noExitRuntime: true,
    })
    .create();

const exports = await getAssemblyExports(getConfig().mainAssemblyName);

try {
    // Never returns normally on emscripten: emscripten_set_main_loop unwinds
    // the stack with the literal 'unwind' exception and the game keeps
    // running from requestAnimationFrame.
    exports.GameEntry.Start();
} catch (err) {
    if (err !== 'unwind' && err?.message !== 'unwind') {
        status.textContent = 'ERROR: ' + err;
        throw err;
    }
}
status.textContent = '';
canvas.focus();
