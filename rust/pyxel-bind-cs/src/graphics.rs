use std::ffi::{c_char, c_void};

use crate::image::image_from_handle;
use crate::{opt_color, opt_f32, opt_str};

#[no_mangle]
pub extern "C" fn pyxel_cls(color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().clear(color);
        Ok(())
    })
}

/// # Safety
/// `out_color` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_pget(x: f32, y: f32, out_color: *mut u8) -> i32 {
    ffi!({
        *out_color = pyxel::pyxel().pixel(x, y);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_pset(x: f32, y: f32, color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().set_pixel(x, y, color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_line(x1: f32, y1: f32, x2: f32, y2: f32, color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().draw_line(x1, y1, x2, y2, color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_rect(x: f32, y: f32, width: f32, height: f32, color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().draw_rect(x, y, width, height, color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_rectb(x: f32, y: f32, width: f32, height: f32, color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().draw_rect_border(x, y, width, height, color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_circ(x: f32, y: f32, radius: f32, color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().draw_circle(x, y, radius, color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_circb(x: f32, y: f32, radius: f32, color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().draw_circle_border(x, y, radius, color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_elli(x: f32, y: f32, width: f32, height: f32, color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().draw_ellipse(x, y, width, height, color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_ellib(x: f32, y: f32, width: f32, height: f32, color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().draw_ellipse_border(x, y, width, height, color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_tri(
    x1: f32,
    y1: f32,
    x2: f32,
    y2: f32,
    x3: f32,
    y3: f32,
    color: u8,
) -> i32 {
    ffi!({
        pyxel::pyxel().draw_triangle(x1, y1, x2, y2, x3, y3, color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_trib(
    x1: f32,
    y1: f32,
    x2: f32,
    y2: f32,
    x3: f32,
    y3: f32,
    color: u8,
) -> i32 {
    ffi!({
        pyxel::pyxel().draw_triangle_border(x1, y1, x2, y2, x3, y3, color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_fill(x: f32, y: f32, color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().flood_fill(x, y, color);
        Ok(())
    })
}

/// # Safety
/// `text` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_text(x: f32, y: f32, text: *const c_char, color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().draw_text(x, y, opt_str(text).unwrap_or_default(), color, None);
        Ok(())
    })
}

/// Blits from a source image handle to the screen. `colkey` uses -1 as the
/// None sentinel, `rotate` / `scale` use NaN.
///
/// # Safety
/// `image` must be a live handle returned by one of the `pyxel_image_*` /
/// `pyxel_screen` constructors.
#[no_mangle]
pub unsafe extern "C" fn pyxel_blt(
    x: f32,
    y: f32,
    image: *const c_void,
    u: f32,
    v: f32,
    width: f32,
    height: f32,
    colkey: i32,
    rotate: f32,
    scale: f32,
) -> i32 {
    ffi!({
        let source = image_from_handle(image).clone();
        pyxel::screen().borrow_mut().draw_image(
            x,
            y,
            &source,
            u,
            v,
            width,
            height,
            opt_color(colkey),
            opt_f32(rotate),
            opt_f32(scale),
        );
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_clip(x: f32, y: f32, width: f32, height: f32) -> i32 {
    ffi!({
        pyxel::pyxel().set_clip_rect(x, y, width, height);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_clip_reset() -> i32 {
    ffi!({
        pyxel::pyxel().reset_clip_rect();
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_camera(x: f32, y: f32) -> i32 {
    ffi!({
        pyxel::pyxel().set_camera(x, y);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_camera_reset() -> i32 {
    ffi!({
        pyxel::pyxel().reset_camera();
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_dither(alpha: f32) -> i32 {
    ffi!({
        pyxel::pyxel().set_dithering(alpha);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_pal(src_color: u8, dst_color: u8) -> i32 {
    ffi!({
        pyxel::pyxel().map_color(src_color, dst_color);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_pal_reset() -> i32 {
    ffi!({
        pyxel::pyxel().reset_color_map();
        Ok(())
    })
}
