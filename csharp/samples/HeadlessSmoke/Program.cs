// FFI smoke test: runs pyxel-core headless (no window) and verifies that
// drawing and reading pixels round-trips through the binding.
using PyxelSharp;

Pyxel.Init(64, 48, title: "smoke", headless: true);

Check(Pyxel.Width == 64, $"Width = {Pyxel.Width}");
Check(Pyxel.Height == 48, $"Height = {Pyxel.Height}");

Pyxel.Cls(Color.Navy);
Check(Pyxel.Pget(10, 10) == Color.Navy, $"cls -> pget = {Pyxel.Pget(10, 10)}");

Pyxel.Rect(5, 5, 10, 10, Color.Red);
Check(Pyxel.Pget(7, 7) == Color.Red, $"rect -> pget = {Pyxel.Pget(7, 7)}");
Check(Pyxel.Pget(20, 20) == Color.Navy, $"outside rect -> pget = {Pyxel.Pget(20, 20)}");

Pyxel.Pset(0, 0, Color.White);
Check(Pyxel.Pget(0, 0) == Color.White, $"pset -> pget = {Pyxel.Pget(0, 0)}");

Pyxel.Text(2, 20, "OK", Color.Yellow);
Check(!Pyxel.Btn(Key.Space), "btn(Space) is false");

// Stage 2: image handles

Check(Pyxel.Screen.Width == 64, $"Screen.Width = {Pyxel.Screen.Width}");
Check(Pyxel.Screen.Pget(0, 0) == Color.White, $"Screen.Pget = {Pyxel.Screen.Pget(0, 0)}");

var image = new Image(8, 8);
Check(image.Width == 8 && image.Height == 8, $"new Image -> {image.Width}x{image.Height}");
image.Cls(Color.Green);
image.Pset(1, 1, Color.Yellow);
Check(image.Pget(1, 1) == Color.Yellow, $"image pset -> pget = {image.Pget(1, 1)}");
Check(image.Data[image.Width + 1] == Color.Yellow.Value, "image Data span matches pset");

Pyxel.Blt(30, 30, image, 0, 0, 8, 8);
Check(Pyxel.Pget(31, 31) == Color.Yellow, $"blt -> screen pget = {Pyxel.Pget(31, 31)}");

// colkey masks out matching pixels; (45, 34) keeps the earlier Navy clear
Pyxel.Blt(44, 30, image, 0, 0, 8, 8, colorKey: Color.Green);
Check(Pyxel.Pget(45, 34) == Color.Navy, $"blt colkey -> screen pget = {Pyxel.Pget(45, 34)}");
Pyxel.Blt(20, 5, image, 0, 0, 8, 8, rotate: 90, scale: 1.5f);

Check(Pyxel.Images.Count == 3, $"Images.Count = {Pyxel.Images.Count}");
Pyxel.Images[0].Set(0, 0, "0123", "4567");
Check(Pyxel.Images[0].Pget(2, 0) == 2, $"bank set -> pget = {Pyxel.Images[0].Pget(2, 0)}");
Check(Pyxel.Images[0].Pget(3, 1) == 7, $"bank set -> pget = {Pyxel.Images[0].Pget(3, 1)}");
Check(ReferenceEquals(Pyxel.Images[0], Pyxel.Images[0]), "bank wrappers are cached");

// save -> load round-trip without external assets
var savePath = Path.Combine(Path.GetTempPath(), "pyxel_smoke.png");
image.Save(savePath);
var loaded = Image.FromImage(savePath);
Check(loaded.Width == 8 && loaded.Height == 8, $"FromImage -> {loaded.Width}x{loaded.Height}");
Check(loaded.Pget(1, 1) == Color.Yellow, $"FromImage -> pget = {loaded.Pget(1, 1)}");
Pyxel.Images[1].Load(0, 0, savePath);
Check(Pyxel.Images[1].Pget(1, 1) == Color.Yellow, $"bank load -> pget = {Pyxel.Images[1].Pget(1, 1)}");
loaded.Dispose();
File.Delete(savePath);

// error paths: engine errors and pyxel-core panics both become PyxelException
try
{
    Pyxel.Images[0].Load(0, 0, "no_such_file.png");
    Check(false, "load of missing file should throw");
}
catch (PyxelException exception)
{
    Check(true, $"missing file -> PyxelException ({Truncate(exception.Message)})");
}
try
{
    image.Blt(0, 0, image, 0, 0, 4, 4); // RefCell double borrow panics in pyxel-core
    Check(false, "self-blt should throw");
}
catch (PyxelException exception)
{
    Check(true, $"panic -> PyxelException ({Truncate(exception.Message)})");
}

image.Dispose();
try
{
    image.Pget(0, 0);
    Check(false, "disposed image should throw");
}
catch (ObjectDisposedException)
{
    Check(true, "disposed image -> ObjectDisposedException");
}

// Stage 2 slice 2: tilemaps

Check(Pyxel.Tilemaps.Count == 8, $"Tilemaps.Count = {Pyxel.Tilemaps.Count}");

var tilemap = new Tilemap(4, 4, 0);
Check(tilemap.Width == 4 && tilemap.Height == 4, $"new Tilemap -> {tilemap.Width}x{tilemap.Height}");
tilemap.Cls((1, 1));
Check(tilemap.Pget(0, 0) == new Tile(1, 1), $"tilemap cls -> pget = {tilemap.Pget(0, 0)}");
tilemap.Pset(0, 0, (0, 0));
Check(tilemap.Pget(0, 0) == new Tile(0, 0), $"tilemap pset -> pget = {tilemap.Pget(0, 0)}");

// tile (0, 0) references image bank 0 pixels (0..8, 0..8), which still hold
// the "0123"/"4567" pattern written above
Pyxel.Bltm(50, 20, tilemap, 0, 0, 8, 8);
Check(Pyxel.Pget(52, 20) == 2, $"bltm -> screen pget = {Pyxel.Pget(52, 20)}");

var (allowedDx, allowedDy) = tilemap.Collide(0, 0, 8, 8, 4, 0);
Check(allowedDx == 4 && allowedDy == 0, $"collide without walls -> ({allowedDx}, {allowedDy})");

Check(tilemap.ImageSourceIndex == 0, $"imgsrc index = {tilemap.ImageSourceIndex}");
var sourceImage = new Image(8, 8);
tilemap.SetImageSource(sourceImage);
Check(tilemap.ImageSourceIndex is null, "imgsrc index is null after image source");
var readBack = tilemap.ImageSourceImage;
Check(readBack is { Width: 8 }, "imgsrc image reads back");
readBack!.Dispose();
tilemap.SetImageSource(0);
sourceImage.Dispose();
tilemap.Dispose();

// Stage 2 slice 2: fonts

var fontPath = Path.Combine(AppContext.BaseDirectory, "assets", "PixelMplus10-Regular.ttf");
using (var font = new Font(fontPath, 10))
{
    Check(font.TextWidth("Hello") > 0, $"font text width = {font.TextWidth("Hello")}");
    Pyxel.Text(2, 40, "F", Color.White, font);
    Check(true, "text with font draws");
}

// Stage 2 slice 2: resources

var shotPath = Path.Combine(Path.GetTempPath(), "pyxel_smoke_shot.png");
Pyxel.Screenshot(shotPath);
Check(File.Exists(shotPath), "screenshot writes a file");
File.Delete(shotPath);

var dataDir = Pyxel.UserDataDir("PyxelSharp", "HeadlessSmoke");
Check(!string.IsNullOrEmpty(dataDir), $"user data dir = {dataDir}");

var pyxresPath = Path.Combine(AppContext.BaseDirectory, "assets", "jump_game.pyxres");
Pyxel.Load(pyxresPath);
var bankHasPixels = false;
foreach (var value in Pyxel.Images[0].Data)
{
    if (value != 0)
    {
        bankHasPixels = true;
        break;
    }
}
Check(bankHasPixels, "pyxres load populates image bank 0");

// Stage 3: math

Pyxel.Rseed(42);
var firstRoll = Pyxel.Rndi(0, 100);
Pyxel.Rseed(42);
Check(Pyxel.Rndi(0, 100) == firstRoll, "rseed makes rndi deterministic");
var randomFloat = Pyxel.Rndf(0, 1);
Check(randomFloat is >= 0 and <= 1, $"rndf in range = {randomFloat}");
Check(Pyxel.Ceil(1.2f) == 2 && Pyxel.Floor(1.8f) == 1, "ceil/floor");
Check(Math.Abs(Pyxel.Cos(0) - 1) < 1e-4, $"cos(0) = {Pyxel.Cos(0)}");
Check(Math.Abs(Pyxel.Sqrt(4) - 2) < 1e-4, $"sqrt(4) = {Pyxel.Sqrt(4)}");
Check(Math.Abs(Math.Abs(Pyxel.Sin(90)) - 1) < 1e-4, $"sin(90) = {Pyxel.Sin(90)}");
Pyxel.Nseed(1);
Check(!float.IsNaN(Pyxel.Noise(0.5f, 0.5f)), $"noise = {Pyxel.Noise(0.5f, 0.5f)}");

// Stage 3: sounds and music

var sound = new Sound();
sound.Set("c2d2e2f2", "s", "6", "n", 20);
Check(sound.Notes.Length == 4, $"sound set -> notes = {sound.Notes.Length}");
Check(sound.Speed == 20, $"sound speed = {sound.Speed}");
sound.Notes = [0, 2, 4];
Check(sound.Notes is [0, 2, 4], "sound notes write/read round-trips");
sound.Speed = 15;
Check(sound.Speed == 15, $"sound speed set -> {sound.Speed}");
Check(sound.TotalSec() is > 0, $"sound total sec = {sound.TotalSec()}");
sound.Mml("T120 O4 L8 CDEF");
sound.Mml(null);
Check(true, "sound MML set and clear");
sound.Dispose();

var music = new Music();
music.Set([0, 1, 2], [3]);
Check(music.Seqs.Length == 2 && music.Seqs[0][2] == 2 && music.Seqs[1][0] == 3,
    "music seqs round-trip");
music.Dispose();

Check(Pyxel.Sounds.Count == 64, $"Sounds.Count = {Pyxel.Sounds.Count}");
Check(Pyxel.Musics.Count == 8, $"Musics.Count = {Pyxel.Musics.Count}");
Check(Pyxel.Channels.Count == 4, $"Channels.Count = {Pyxel.Channels.Count}");
Check(Pyxel.Tones.Count == 4, $"Tones.Count = {Pyxel.Tones.Count}");

Check(Pyxel.Sounds[0].Notes.Length > 0, "pyxres load populates sound bank 0");

Pyxel.Channels[0].Gain = 0.5f;
Check(Math.Abs(Pyxel.Channels[0].Gain - 0.5f) < 1e-6, $"channel gain = {Pyxel.Channels[0].Gain}");
Check(Pyxel.Tones[0].Mode == ToneMode.Wavetable, $"tone mode = {Pyxel.Tones[0].Mode}");
Check(Pyxel.Tones[0].Wavetable.Length > 0, $"tone wavetable = {Pyxel.Tones[0].Wavetable.Length}");

// Playback in headless mode: just verify the calls succeed
Pyxel.Play(3, 3);
Pyxel.Play(0, "T120 O4 L8 CDEF");
Pyxel.Playm(0);
Pyxel.Stop(3);
Pyxel.Stop();
Check(true, "play/playm/stop succeed headless");

// Stage 3: input

Pyxel.SetInputText("abc");
Check(Pyxel.InputText == "abc", $"input text = '{Pyxel.InputText}'");
Pyxel.SetDroppedFiles(["a.txt", "b.txt"]);
Check(Pyxel.DroppedFiles is ["a.txt", "b.txt"], "dropped files round-trip");
Pyxel.SetBtn(Key.Space, true);
Check(Pyxel.Btn(Key.Space), "set_btn -> btn");
Pyxel.SetBtn(Key.Space, false);

// Stage 3: colors and resize

Check(Pyxel.Colors.Count >= 16, $"Colors.Count = {Pyxel.Colors.Count}");
var savedColor = Pyxel.Colors[15];
Pyxel.Colors[15] = 0x123456;
Check(Pyxel.Colors[15] == 0x123456, "colors set/get round-trips");
Pyxel.Colors[15] = savedColor;

Pyxel.Resize(80, 60);
Check(Pyxel.Width == 80 && Pyxel.Height == 60, $"resize -> {Pyxel.Width}x{Pyxel.Height}");

// Constants: hardcoded values must agree with the linked pyxel-core

Check(Pyxel.Version.Length > 0, $"version = {Pyxel.Version}");
Check(Pyxel.NumColors == Pyxel.Colors.Count, "NumColors matches runtime");
Check(Pyxel.NumImages == Pyxel.Images.Count, "NumImages matches runtime");
Check(Pyxel.NumTilemaps == Pyxel.Tilemaps.Count, "NumTilemaps matches runtime");
Check(Pyxel.NumChannels == Pyxel.Channels.Count, "NumChannels matches runtime");
Check(Pyxel.NumTones == Pyxel.Tones.Count, "NumTones matches runtime");
Check(Pyxel.NumSounds == Pyxel.Sounds.Count, "NumSounds matches runtime");
Check(Pyxel.NumMusics == Pyxel.Musics.Count, "NumMusics matches runtime");
Check(Pyxel.DefaultColors.Length == Pyxel.NumColors, "DefaultColors length");
Check(Pyxel.Images[0].Width == Pyxel.ImageSize, "ImageSize matches bank width");
Check(Pyxel.Tilemaps[0].Width == Pyxel.TilemapSize, "TilemapSize matches bank width");

Console.WriteLine("HeadlessSmoke: all checks passed");
return 0;

static string Truncate(string message) =>
    message.Length <= 60 ? message : message[..60] + "...";

static void Check(bool condition, string what)
{
    if (!condition)
    {
        Console.Error.WriteLine($"FAILED: {what}");
        Environment.Exit(1);
    }
    Console.WriteLine($"ok: {what}");
}
