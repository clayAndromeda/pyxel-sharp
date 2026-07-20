// Shared boot glue for PyxelSharp browser-wasm games.
//
// Usage from a host page (see csharp/samples/*.Web/wwwroot/index.html):
//
//   <script type="module">
//     import { bootPyxel } from './pyxel-boot.js';
//     await bootPyxel({
//       clickToStart: true, // true when the game plays audio
//       // Fetched over HTTP and written into the emscripten MEMFS before the
//       // game starts, so Pyxel.Load(path) can read them:
//       assets: [{ url: './assets/my_resource.pyxres', path: '/assets/my_resource.pyxres' }],
//     });
//   </script>
//
// Expects the page to contain <canvas id="canvas"> (with an explicit CSS
// size: emscripten's SDL2 uses the canvas CSS size as the framebuffer size)
// and optionally <p id="status"> for progress/error text.
//
// The game assembly must expose a [JSExport] entry point:
//
//   public static partial class GameEntry { [JSExport] public static void Start() ... }

import { dotnet } from './_framework/dotnet.js';

export async function bootPyxel(options = {}) {
    const canvas = options.canvas ?? document.getElementById('canvas');
    const status = options.statusEl ?? document.getElementById('status');
    const setStatus = text => { if (status) status.textContent = text; };

    // JS hooks that pyxel-core's emscripten build expects the hosting page to
    // provide (upstream's pyxel.js defines these; we stub them). Missing hooks
    // fail the first frame with a silently-swallowed ReferenceError.
    window._readVirtualGamepadBitmask ??= () => 0; // virtual gamepad: none
    window._scanCorrection ??= [];                 // non-US key correction: off
    window.resetPyxel ??= () => location.reload();

    // Testing aid: drive the main loop with setTimeout when the tab is hidden
    // (browsers stop requestAnimationFrame for hidden tabs). Used by headless
    // verification; harmless otherwise.
    if (new URLSearchParams(location.search).has('forceRaf')) {
        window.requestAnimationFrame = cb => setTimeout(() => cb(performance.now()), 33);
        window.cancelAnimationFrame = id => clearTimeout(id);
    }

    setStatus('loading .NET runtime...');
    const { getAssemblyExports, getConfig } = await dotnet
        .withModuleConfig({
            canvas,
            // Keep the emscripten runtime alive after Start() unwinds into the
            // browser-driven main loop.
            noExitRuntime: true,
        })
        .create();

    const exports = await getAssemblyExports(getConfig().mainAssemblyName);

    if (options.assets?.length) {
        setStatus('loading assets...');
        await Promise.all(options.assets.map(async asset => {
            const res = await fetch(asset.url);
            if (!res.ok) {
                throw new Error(`failed to fetch ${asset.url}: HTTP ${res.status}`);
            }
            const bytes = new Uint8Array(await res.arrayBuffer());
            exports.PyxelWebHost.WriteFile(asset.path, bytes);
        }));
    }

    const start = () => {
        try {
            // Never returns normally on emscripten: emscripten_set_main_loop
            // unwinds the stack with the literal 'unwind' exception and the
            // game keeps running from requestAnimationFrame.
            exports.GameEntry.Start();
        } catch (err) {
            if (err !== 'unwind' && err?.message !== 'unwind') {
                setStatus('ERROR: ' + err);
                throw err;
            }
        }
        setStatus('');
        canvas.focus();
    };

    if (options.clickToStart) {
        // Browsers only allow audio to start inside a user gesture, so games
        // with sound must defer Pyxel.Init/Run to a click (upstream Pyxel has
        // the same constraint).
        setStatus('CLICK TO START');
        canvas.style.cursor = 'pointer';
        document.body.addEventListener('click', () => {
            canvas.style.cursor = '';
            start();
        }, { once: true });
    } else {
        start();
    }
}
