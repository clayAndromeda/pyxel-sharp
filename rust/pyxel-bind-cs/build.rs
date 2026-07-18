fn main() {
    // SDL2 static build on Windows needs these system libraries; pyxel-core's
    // build script does not emit all of them for a cdylib consumer.
    let target_os = std::env::var("CARGO_CFG_TARGET_OS").unwrap_or_default();
    if target_os == "windows" {
        for lib in [
            "advapi32", "setupapi", "winmm", "imm32", "version", "ole32", "oleaut32", "uuid",
            "gdi32", "user32", "shell32",
        ] {
            println!("cargo:rustc-link-lib={lib}");
        }
    }

    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .input_extern_file("src/system.rs")
        .input_extern_file("src/audio.rs")
        .input_extern_file("src/channel.rs")
        .input_extern_file("src/font.rs")
        .input_extern_file("src/graphics.rs")
        .input_extern_file("src/image.rs")
        .input_extern_file("src/input.rs")
        .input_extern_file("src/math.rs")
        .input_extern_file("src/music.rs")
        .input_extern_file("src/resource.rs")
        .input_extern_file("src/sound.rs")
        .input_extern_file("src/tilemap.rs")
        .input_extern_file("src/tone.rs")
        .csharp_dll_name("pyxel_bind_cs")
        .csharp_namespace("PyxelSharp.Native")
        .csharp_class_name("NativeMethods")
        .generate_csharp_file("../../csharp/src/PyxelSharp/NativeMethods.g.cs")
        .unwrap();
}
