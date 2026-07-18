use std::ffi::c_char;

use pyxel::{Pyxel, PyxelCallback};

use crate::{opt_str, opt_u32, set_last_error, NONE_U32};

/// # Safety
/// `title` must be null or a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_init(
    width: u32,
    height: u32,
    title: *const c_char,
    fps: u32,
    quit_key: u32,
    display_scale: u32,
    capture_scale: u32,
    capture_sec: u32,
    headless: i32,
) -> i32 {
    let headless = match headless {
        -1 => None,
        0 => Some(false),
        _ => Some(true),
    };
    match pyxel::init(
        width,
        height,
        opt_str(title),
        opt_u32(fps),
        (quit_key != NONE_U32).then_some(quit_key),
        opt_u32(display_scale),
        opt_u32(capture_scale),
        opt_u32(capture_sec),
        headless,
    ) {
        Ok(()) => 0,
        Err(message) => {
            set_last_error(&message);
            -1
        }
    }
}

struct FnPtrCallback {
    update: extern "C" fn(),
    draw: extern "C" fn(),
}

impl PyxelCallback for FnPtrCallback {
    fn update(&mut self) {
        (self.update)();
    }

    fn draw(&mut self) {
        (self.draw)();
    }
}

#[no_mangle]
pub extern "C" fn pyxel_run(update: extern "C" fn(), draw: extern "C" fn()) {
    Pyxel::run(FnPtrCallback { update, draw });
}

#[no_mangle]
pub extern "C" fn pyxel_show() {
    Pyxel::show_screen();
}

#[no_mangle]
pub extern "C" fn pyxel_flip() {
    Pyxel::flip_screen();
}

#[no_mangle]
pub extern "C" fn pyxel_quit() {
    Pyxel::quit();
}

/// # Safety
/// `title` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_title(title: *const c_char) {
    pyxel::pyxel().set_title(opt_str(title).unwrap_or_default());
}

#[no_mangle]
pub extern "C" fn pyxel_fullscreen(enabled: bool) {
    pyxel::pyxel().set_fullscreen(enabled);
}

#[no_mangle]
pub extern "C" fn pyxel_perf_monitor(enabled: bool) {
    pyxel::pyxel().set_perf_monitor(enabled);
}

#[no_mangle]
pub extern "C" fn pyxel_frame_count() -> u32 {
    *pyxel::frame_count()
}

#[no_mangle]
pub extern "C" fn pyxel_width() -> u32 {
    *pyxel::width()
}

#[no_mangle]
pub extern "C" fn pyxel_height() -> u32 {
    *pyxel::height()
}
