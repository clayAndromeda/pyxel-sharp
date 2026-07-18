use std::ffi::c_char;

use crate::opt_str;

#[no_mangle]
pub extern "C" fn pyxel_cls(color: u8) {
    pyxel::pyxel().clear(color);
}

#[no_mangle]
pub extern "C" fn pyxel_pget(x: f32, y: f32) -> u8 {
    pyxel::pyxel().pixel(x, y)
}

#[no_mangle]
pub extern "C" fn pyxel_pset(x: f32, y: f32, color: u8) {
    pyxel::pyxel().set_pixel(x, y, color);
}

#[no_mangle]
pub extern "C" fn pyxel_line(x1: f32, y1: f32, x2: f32, y2: f32, color: u8) {
    pyxel::pyxel().draw_line(x1, y1, x2, y2, color);
}

#[no_mangle]
pub extern "C" fn pyxel_rect(x: f32, y: f32, width: f32, height: f32, color: u8) {
    pyxel::pyxel().draw_rect(x, y, width, height, color);
}

#[no_mangle]
pub extern "C" fn pyxel_rectb(x: f32, y: f32, width: f32, height: f32, color: u8) {
    pyxel::pyxel().draw_rect_border(x, y, width, height, color);
}

#[no_mangle]
pub extern "C" fn pyxel_circ(x: f32, y: f32, radius: f32, color: u8) {
    pyxel::pyxel().draw_circle(x, y, radius, color);
}

#[no_mangle]
pub extern "C" fn pyxel_circb(x: f32, y: f32, radius: f32, color: u8) {
    pyxel::pyxel().draw_circle_border(x, y, radius, color);
}

#[no_mangle]
pub extern "C" fn pyxel_elli(x: f32, y: f32, width: f32, height: f32, color: u8) {
    pyxel::pyxel().draw_ellipse(x, y, width, height, color);
}

#[no_mangle]
pub extern "C" fn pyxel_ellib(x: f32, y: f32, width: f32, height: f32, color: u8) {
    pyxel::pyxel().draw_ellipse_border(x, y, width, height, color);
}

#[no_mangle]
pub extern "C" fn pyxel_tri(x1: f32, y1: f32, x2: f32, y2: f32, x3: f32, y3: f32, color: u8) {
    pyxel::pyxel().draw_triangle(x1, y1, x2, y2, x3, y3, color);
}

#[no_mangle]
pub extern "C" fn pyxel_trib(x1: f32, y1: f32, x2: f32, y2: f32, x3: f32, y3: f32, color: u8) {
    pyxel::pyxel().draw_triangle_border(x1, y1, x2, y2, x3, y3, color);
}

/// # Safety
/// `text` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_text(x: f32, y: f32, text: *const c_char, color: u8) {
    pyxel::pyxel().draw_text(x, y, opt_str(text).unwrap_or_default(), color, None);
}

#[no_mangle]
pub extern "C" fn pyxel_clip(x: f32, y: f32, width: f32, height: f32) {
    pyxel::pyxel().set_clip_rect(x, y, width, height);
}

#[no_mangle]
pub extern "C" fn pyxel_clip_reset() {
    pyxel::pyxel().reset_clip_rect();
}

#[no_mangle]
pub extern "C" fn pyxel_camera(x: f32, y: f32) {
    pyxel::pyxel().set_camera(x, y);
}

#[no_mangle]
pub extern "C" fn pyxel_camera_reset() {
    pyxel::pyxel().reset_camera();
}

#[no_mangle]
pub extern "C" fn pyxel_dither(alpha: f32) {
    pyxel::pyxel().set_dithering(alpha);
}

#[no_mangle]
pub extern "C" fn pyxel_pal(src_color: u8, dst_color: u8) {
    pyxel::pyxel().map_color(src_color, dst_color);
}

#[no_mangle]
pub extern "C" fn pyxel_pal_reset() {
    pyxel::pyxel().reset_color_map();
}

