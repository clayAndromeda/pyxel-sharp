use std::ffi::c_char;

use pyxel::{Pyxel, PyxelCallback};

use crate::{opt_bool, opt_str, opt_u32, NONE_U32};

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
    ffi!(pyxel::init(
        width,
        height,
        opt_str(title),
        opt_u32(fps),
        (quit_key != NONE_U32).then_some(quit_key),
        opt_u32(display_scale),
        opt_u32(capture_scale),
        opt_u32(capture_sec),
        opt_bool(headless),
    ))
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
pub extern "C" fn pyxel_run(update: extern "C" fn(), draw: extern "C" fn()) -> i32 {
    ffi!({
        Pyxel::run(FnPtrCallback { update, draw });
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_show() -> i32 {
    ffi!({
        Pyxel::show_screen();
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_flip() -> i32 {
    ffi!({
        Pyxel::flip_screen();
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_quit() -> i32 {
    ffi!({
        Pyxel::quit();
        Ok(())
    })
}

/// # Safety
/// `title` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_title(title: *const c_char) -> i32 {
    ffi!({
        pyxel::pyxel().set_title(opt_str(title).unwrap_or_default());
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_fullscreen(enabled: bool) -> i32 {
    ffi!({
        pyxel::pyxel().set_fullscreen(enabled);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_perf_monitor(enabled: bool) -> i32 {
    ffi!({
        pyxel::pyxel().set_perf_monitor(enabled);
        Ok(())
    })
}

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_frame_count(out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = *pyxel::frame_count();
        Ok(())
    })
}

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_width(out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = *pyxel::width();
        Ok(())
    })
}

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_height(out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = *pyxel::height();
        Ok(())
    })
}
