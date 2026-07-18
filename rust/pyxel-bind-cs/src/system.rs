use std::ffi::c_char;

use pyxel::{Pyxel, PyxelCallback};

use crate::{opt_bool, opt_str, opt_u32, set_string_result, NONE_U32};

/// The linked pyxel-core version (Python: `pyxel.VERSION`). The pointer
/// written to `out_version` stays valid until the next string-returning call
/// on this thread.
///
/// # Safety
/// `out_version` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_version(out_version: *mut *const c_char) -> i32 {
    ffi!({
        *out_version = set_string_result(pyxel::VERSION.to_string());
        Ok(())
    })
}

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

/// Restarts the application (Python: `pyxel.reset`).
#[no_mangle]
pub extern "C" fn pyxel_reset() -> i32 {
    ffi!({
        Pyxel::restart();
        Ok(())
    })
}

/// Sets the window icon from rows of hex color digits
/// (Python: `pyxel.icon`). `colkey` uses -1 as the None sentinel.
///
/// # Safety
/// `data` must point to `data_len` valid NUL-terminated UTF-8 strings.
#[no_mangle]
pub unsafe extern "C" fn pyxel_icon(
    data: *const *const c_char,
    data_len: u32,
    scale: u32,
    colkey: i32,
) -> i32 {
    ffi!({
        let rows: Vec<&str> = (0..data_len as usize)
            .map(|i| opt_str(*data.add(i)).unwrap_or_default())
            .collect();
        pyxel::pyxel().set_icon(&rows, scale, crate::opt_color(colkey))?;
        Ok(())
    })
}

/// Restricts display scaling to integer factors (Python: `pyxel.integer_scale`).
#[no_mangle]
pub extern "C" fn pyxel_integer_scale(enabled: bool) -> i32 {
    ffi!({
        pyxel::pyxel().set_integer_scale(enabled);
        Ok(())
    })
}

/// Switches the screen shader mode (Python: `pyxel.screen_mode`).
#[no_mangle]
pub extern "C" fn pyxel_screen_mode(screen_mode: u32) -> i32 {
    ffi!({
        pyxel::pyxel().set_screen_mode(screen_mode);
        Ok(())
    })
}

/// Resizes the screen (Python: `pyxel.resize`).
#[no_mangle]
pub extern "C" fn pyxel_resize(width: u32, height: u32) -> i32 {
    ffi!({
        pyxel::pyxel().set_screen_size(width, height)?;
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
