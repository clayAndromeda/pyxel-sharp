// Windows shim: pyxel-core's build.rs invokes Command::new("emcc"), which
// cannot spawn emcc.bat, so this exe forwards all arguments to it.
// tools/Build-Wasm.ps1 builds this and prepends it to PATH.
using System.Diagnostics;

var emsdk = Environment.GetEnvironmentVariable("EMSDK");
if (string.IsNullOrEmpty(emsdk))
{
    Console.Error.WriteLine("emcc shim: EMSDK environment variable is not set");
    return 1;
}
var emccBat = Path.Combine(emsdk, "upstream", "emscripten", "emcc.bat");

var psi = new ProcessStartInfo("cmd.exe") { UseShellExecute = false };
psi.ArgumentList.Add("/d");
psi.ArgumentList.Add("/c");
psi.ArgumentList.Add(emccBat);
foreach (var arg in args)
{
    psi.ArgumentList.Add(arg);
}

var process = Process.Start(psi)!;
process.WaitForExit();
return process.ExitCode;
