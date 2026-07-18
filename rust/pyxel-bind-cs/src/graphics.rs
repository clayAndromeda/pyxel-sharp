use std::ffi::{c_char, c_void};

use crate::font::opt_font;
use crate::image::image_from_handle;
use crate::tilemap::tilemap_from_handle;
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
/// `text` must be a valid NUL-terminated UTF-8 string and `font` null
/// (built-in font) or a live font handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_text(
    x: f32,
    y: f32,
    text: *const c_char,
    color: u8,
    font: *const c_void,
) -> i32 {
    ffi!({
        pyxel::pyxel().draw_text(x, y, opt_str(text).unwrap_or_default(), color, opt_font(font));
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

/// Draws a region of a tilemap onto the screen (Python: `pyxel.bltm`).
/// `colkey` uses -1 as the None sentinel, `rotate` / `scale` use NaN.
///
/// # Safety
/// `tilemap` must be a live tilemap handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_bltm(
    x: f32,
    y: f32,
    tilemap: *const c_void,
    u: f32,
    v: f32,
    width: f32,
    height: f32,
    colkey: i32,
    rotate: f32,
    scale: f32,
) -> i32 {
    ffi!({
        let tilemap = tilemap_from_handle(tilemap).clone();
        pyxel::screen().borrow_mut().draw_tilemap(
            x,
            y,
            &tilemap,
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

/// Perspective-projects a region of an image onto the screen
/// (Python: `pyxel.blt3d`). `fov` uses NaN and `colkey` -1 as None sentinels.
///
/// # Safety
/// `image` must be a live image handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_blt3d(
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    image: *const c_void,
    pos_x: f32,
    pos_y: f32,
    pos_z: f32,
    rot_x: f32,
    rot_y: f32,
    rot_z: f32,
    fov: f32,
    colkey: i32,
) -> i32 {
    ffi!({
        let source = image_from_handle(image).clone();
        pyxel::screen().borrow_mut().draw_image_3d(
            x,
            y,
            width,
            height,
            &source,
            (pos_x, pos_y, pos_z),
            (rot_x, rot_y, rot_z),
            opt_f32(fov),
            opt_color(colkey),
        );
        Ok(())
    })
}

/// Perspective-projects a region of a tilemap onto the screen
/// (Python: `pyxel.bltm3d`). `fov` uses NaN and `colkey` -1 as None sentinels.
///
/// # Safety
/// `tilemap` must be a live tilemap handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_bltm3d(
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    tilemap: *const c_void,
    pos_x: f32,
    pos_y: f32,
    pos_z: f32,
    rot_x: f32,
    rot_y: f32,
    rot_z: f32,
    fov: f32,
    colkey: i32,
) -> i32 {
    ffi!({
        let tilemap = tilemap_from_handle(tilemap).clone();
        pyxel::screen().borrow_mut().draw_tilemap_3d(
            x,
            y,
            width,
            height,
            &tilemap,
            (pos_x, pos_y, pos_z),
            (rot_x, rot_y, rot_z),
            opt_f32(fov),
            opt_color(colkey),
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

/// Number of display palette entries (Python: `len(pyxel.colors)`).
///
/// # Safety
/// `out_len` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_colors_len(out_len: *mut u32) -> i32 {
    ffi!({
        *out_len = pyxel::colors().len() as u32;
        Ok(())
    })
}

/// Reads display palette entry `index` as 0xRRGGBB
/// (Python: `pyxel.colors[i]`).
///
/// # Safety
/// `out_rgb` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_colors_get(index: u32, out_rgb: *mut u32) -> i32 {
    ffi!({
        *out_rgb = *pyxel::colors()
            .get(index as usize)
            .ok_or_else(|| format!("color index out of range: {index}"))?;
        Ok(())
    })
}

/// Writes display palette entry `index` as 0xRRGGBB
/// (Python: `pyxel.colors[i] = rgb`).
#[no_mangle]
pub extern "C" fn pyxel_colors_set(index: u32, rgb: u32) -> i32 {
    ffi!({
        *pyxel::colors()
            .get_mut(index as usize)
            .ok_or_else(|| format!("color index out of range: {index}"))? = rgb;
        Ok(())
    })
}
