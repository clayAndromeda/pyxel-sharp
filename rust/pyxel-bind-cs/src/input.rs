use crate::opt_u32;

#[no_mangle]
pub extern "C" fn pyxel_btn(key: u32) -> bool {
    pyxel::pyxel().is_button_down(key)
}

/// `hold_frames` / `repeat_frames` use `u32::MAX` as the None sentinel.
#[no_mangle]
pub extern "C" fn pyxel_btnp(key: u32, hold_frames: u32, repeat_frames: u32) -> bool {
    pyxel::pyxel().is_button_pressed(key, opt_u32(hold_frames), opt_u32(repeat_frames))
}

#[no_mangle]
pub extern "C" fn pyxel_btnr(key: u32) -> bool {
    pyxel::pyxel().is_button_released(key)
}

#[no_mangle]
pub extern "C" fn pyxel_btnv(key: u32) -> i32 {
    pyxel::pyxel().button_value(key)
}

#[no_mangle]
pub extern "C" fn pyxel_mouse_x() -> i32 {
    *pyxel::mouse_x()
}

#[no_mangle]
pub extern "C" fn pyxel_mouse_y() -> i32 {
    *pyxel::mouse_y()
}

#[no_mangle]
pub extern "C" fn pyxel_mouse_wheel() -> i32 {
    *pyxel::mouse_wheel()
}

#[no_mangle]
pub extern "C" fn pyxel_mouse(visible: bool) {
    pyxel::pyxel().set_mouse_visible(visible);
}

#[no_mangle]
pub extern "C" fn pyxel_warp_mouse(x: f32, y: f32) {
    pyxel::pyxel().set_mouse_position(x, y);
}
