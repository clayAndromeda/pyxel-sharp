use std::ffi::c_char;

use crate::{opt_str, opt_u32, set_string_result};

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_btn(key: u32, out_value: *mut bool) -> i32 {
    ffi!({
        *out_value = pyxel::pyxel().is_button_down(key);
        Ok(())
    })
}

/// `hold_frames` / `repeat_frames` use `u32::MAX` as the None sentinel.
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_btnp(
    key: u32,
    hold_frames: u32,
    repeat_frames: u32,
    out_value: *mut bool,
) -> i32 {
    ffi!({
        *out_value =
            pyxel::pyxel().is_button_pressed(key, opt_u32(hold_frames), opt_u32(repeat_frames));
        Ok(())
    })
}

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_btnr(key: u32, out_value: *mut bool) -> i32 {
    ffi!({
        *out_value = pyxel::pyxel().is_button_released(key);
        Ok(())
    })
}

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_btnv(key: u32, out_value: *mut i32) -> i32 {
    ffi!({
        *out_value = pyxel::pyxel().button_value(key);
        Ok(())
    })
}

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_mouse_x(out_value: *mut i32) -> i32 {
    ffi!({
        *out_value = *pyxel::mouse_x();
        Ok(())
    })
}

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_mouse_y(out_value: *mut i32) -> i32 {
    ffi!({
        *out_value = *pyxel::mouse_y();
        Ok(())
    })
}

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_mouse_wheel(out_value: *mut i32) -> i32 {
    ffi!({
        *out_value = *pyxel::mouse_wheel();
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_mouse(visible: bool) -> i32 {
    ffi!({
        pyxel::pyxel().set_mouse_visible(visible);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_warp_mouse(x: f32, y: f32) -> i32 {
    ffi!({
        pyxel::pyxel().set_mouse_position(x, y);
        Ok(())
    })
}

/// Text typed since the last frame (Python: `pyxel.input_text`). The pointer
/// written to `out_text` stays valid until the next string-returning call on
/// this thread.
///
/// # Safety
/// `out_text` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_input_text(out_text: *mut *const c_char) -> i32 {
    ffi!({
        *out_text = set_string_result(pyxel::input_text().clone());
        Ok(())
    })
}

/// Number of keys currently held (Python: `len(pyxel.input_keys)`).
///
/// # Safety
/// `out_len` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_input_keys_len(out_len: *mut u32) -> i32 {
    ffi!({
        *out_len = pyxel::input_keys().len() as u32;
        Ok(())
    })
}

/// Copies up to `buffer_len` held key codes into `buffer`
/// (Python: `pyxel.input_keys`).
///
/// # Safety
/// `buffer` must hold `buffer_len` u32 values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_input_keys_read(buffer: *mut u32, buffer_len: u32) -> i32 {
    ffi!({
        let keys = pyxel::input_keys();
        let count = keys.len().min(buffer_len as usize);
        std::ptr::copy_nonoverlapping(keys.as_ptr(), buffer, count);
        Ok(())
    })
}

/// Number of files dropped onto the window this frame
/// (Python: `len(pyxel.dropped_files)`).
///
/// # Safety
/// `out_len` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_dropped_files_len(out_len: *mut u32) -> i32 {
    ffi!({
        *out_len = pyxel::dropped_files().len() as u32;
        Ok(())
    })
}

/// Path of dropped file `index` (Python: `pyxel.dropped_files[i]`). The
/// pointer written to `out_path` stays valid until the next string-returning
/// call on this thread.
///
/// # Safety
/// `out_path` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_dropped_file(index: u32, out_path: *mut *const c_char) -> i32 {
    ffi!({
        let path = pyxel::dropped_files()
            .get(index as usize)
            .cloned()
            .ok_or_else(|| format!("dropped file index out of range: {index}"))?;
        *out_path = set_string_result(path);
        Ok(())
    })
}

/// Overrides a key state for the current frame (Python: `pyxel.set_btn`,
/// mainly for tests).
#[no_mangle]
pub extern "C" fn pyxel_set_btn(key: u32, state: bool) -> i32 {
    ffi!({
        pyxel::pyxel().set_button_state(key, state);
        Ok(())
    })
}

/// Overrides an analog key value for the current frame
/// (Python: `pyxel.set_btnv`).
#[no_mangle]
pub extern "C" fn pyxel_set_btnv(key: u32, value: i32) -> i32 {
    ffi!({
        pyxel::pyxel().set_button_value(key, value);
        Ok(())
    })
}

/// Overrides the typed text for the current frame
/// (Python: `pyxel.set_input_text`).
///
/// # Safety
/// `text` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_set_input_text(text: *const c_char) -> i32 {
    ffi!({
        pyxel::pyxel().set_input_text(opt_str(text).unwrap_or_default());
        Ok(())
    })
}

/// Overrides the dropped-file list for the current frame
/// (Python: `pyxel.set_dropped_files`).
///
/// # Safety
/// `files` must point to `files_len` valid NUL-terminated UTF-8 strings.
#[no_mangle]
pub unsafe extern "C" fn pyxel_set_dropped_files(
    files: *const *const c_char,
    files_len: u32,
) -> i32 {
    ffi!({
        let files: Vec<String> = (0..files_len as usize)
            .map(|i| opt_str(*files.add(i)).unwrap_or_default().to_string())
            .collect();
        pyxel::pyxel().set_dropped_files(&files);
        Ok(())
    })
}
