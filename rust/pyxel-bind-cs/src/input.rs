use crate::opt_u32;

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
