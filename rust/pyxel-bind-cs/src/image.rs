//! Opaque-handle FFI for `pyxel::Image` (the C# `Image` class).
//!
//! A handle is a `Box<RcImage>` raw pointer: it owns one `Rc` clone, so the
//! underlying image stays alive while either C# or the engine references it.
//! `RcImage` is `Rc<RefCell<_>>` (not thread-safe); handles must only be
//! created and dropped on the thread running pyxel — the C# side guarantees
//! this with its pending-drop queue.

use std::ffi::{c_char, c_void};

use pyxel::RcImage;

use crate::{opt_bool, opt_color, opt_f32, opt_str};

pub(crate) fn image_into_handle(image: RcImage) -> *mut c_void {
    Box::into_raw(Box::new(image)) as *mut c_void
}

/// # Safety
/// `handle` must be a live pointer produced by `image_into_handle`.
pub(crate) unsafe fn image_from_handle<'a>(handle: *const c_void) -> &'a RcImage {
    &*(handle as *const RcImage)
}

/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_new(
    width: u32,
    height: u32,
    out_handle: *mut *mut c_void,
) -> i32 {
    ffi!({
        let image = pyxel::Image::try_new(width, height)?;
        *out_handle = image_into_handle(image);
        Ok(())
    })
}

/// # Safety
/// `filename` must be a valid NUL-terminated UTF-8 string and `out_handle`
/// must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_from_image(
    filename: *const c_char,
    include_colors: i32,
    out_handle: *mut *mut c_void,
) -> i32 {
    ffi!({
        let image =
            pyxel::Image::from_image(opt_str(filename).unwrap_or_default(), opt_bool(include_colors))?;
        *out_handle = image_into_handle(image);
        Ok(())
    })
}

/// Returns a handle to the image bank `pyxel.images[index]`.
///
/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_bank(index: u32, out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        let image = pyxel::images()
            .get(index as usize)
            .cloned()
            .ok_or_else(|| format!("image index out of range: {index}"))?;
        *out_handle = image_into_handle(image);
        Ok(())
    })
}

/// Returns a handle to the screen image `pyxel.screen`.
///
/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_screen(out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        let image = pyxel::screen().clone();
        *out_handle = image_into_handle(image);
        Ok(())
    })
}

/// Number of image banks (`pyxel::NUM_IMAGES`).
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_num_images(out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = pyxel::NUM_IMAGES;
        Ok(())
    })
}

/// Releases the handle's `Rc` clone. Must be called on the pyxel thread.
///
/// # Safety
/// `handle` must be a live handle; it is invalid after this call.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_drop(handle: *mut c_void) -> i32 {
    ffi!({
        drop(Box::from_raw(handle as *mut RcImage));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_width(handle: *const c_void, out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = image_from_handle(handle).borrow().width();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_height(handle: *const c_void, out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = image_from_handle(handle).borrow().height();
        Ok(())
    })
}

/// Pointer to the image's pixel buffer (`width * height` color indices).
/// Valid while the image is alive and its size unchanged.
///
/// # Safety
/// `handle` must be a live handle and `out_ptr` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_data_ptr(
    handle: *const c_void,
    out_ptr: *mut *mut u8,
) -> i32 {
    ffi!({
        *out_ptr = image_from_handle(handle).borrow_mut().data_ptr();
        Ok(())
    })
}

/// Writes rows of hex color digits (`data`, `data_len` strings) at (x, y),
/// like Python's `image.set`.
///
/// # Safety
/// `handle` must be a live handle; `data` must point to `data_len` valid
/// NUL-terminated UTF-8 strings.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_set(
    handle: *const c_void,
    x: i32,
    y: i32,
    data: *const *const c_char,
    data_len: u32,
) -> i32 {
    ffi!({
        let rows: Vec<&str> = (0..data_len as usize)
            .map(|i| opt_str(*data.add(i)).unwrap_or_default())
            .collect();
        image_from_handle(handle).borrow_mut().set(x, y, &rows)?;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `filename` a valid NUL-terminated
/// UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_load(
    handle: *const c_void,
    x: i32,
    y: i32,
    filename: *const c_char,
    include_colors: i32,
) -> i32 {
    ffi!({
        image_from_handle(handle).borrow_mut().load(
            x,
            y,
            opt_str(filename).unwrap_or_default(),
            opt_bool(include_colors),
        )?;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `filename` a valid NUL-terminated
/// UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_save(
    handle: *const c_void,
    filename: *const c_char,
    scale: u32,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow()
            .save(opt_str(filename).unwrap_or_default(), scale)?;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_clip(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .set_clip_rect(x, y, width, height);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_clip_reset(handle: *const c_void) -> i32 {
    ffi!({
        image_from_handle(handle).borrow_mut().reset_clip_rect();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_camera(handle: *const c_void, x: f32, y: f32) -> i32 {
    ffi!({
        image_from_handle(handle).borrow_mut().set_camera(x, y);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_camera_reset(handle: *const c_void) -> i32 {
    ffi!({
        image_from_handle(handle).borrow_mut().reset_camera();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_pal(
    handle: *const c_void,
    src_color: u8,
    dst_color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .map_color(src_color, dst_color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_pal_reset(handle: *const c_void) -> i32 {
    ffi!({
        image_from_handle(handle).borrow_mut().reset_color_map();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_dither(handle: *const c_void, alpha: f32) -> i32 {
    ffi!({
        image_from_handle(handle).borrow_mut().set_dithering(alpha);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_cls(handle: *const c_void, color: u8) -> i32 {
    ffi!({
        image_from_handle(handle).borrow_mut().clear(color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_color` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_pget(
    handle: *const c_void,
    x: f32,
    y: f32,
    out_color: *mut u8,
) -> i32 {
    ffi!({
        *out_color = image_from_handle(handle).borrow().pixel(x, y);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_pset(
    handle: *const c_void,
    x: f32,
    y: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle).borrow_mut().set_pixel(x, y, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_line(
    handle: *const c_void,
    x1: f32,
    y1: f32,
    x2: f32,
    y2: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .draw_line(x1, y1, x2, y2, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_rect(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .draw_rect(x, y, width, height, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_rectb(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .draw_rect_border(x, y, width, height, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_circ(
    handle: *const c_void,
    x: f32,
    y: f32,
    radius: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .draw_circle(x, y, radius, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_circb(
    handle: *const c_void,
    x: f32,
    y: f32,
    radius: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .draw_circle_border(x, y, radius, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_elli(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .draw_ellipse(x, y, width, height, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_ellib(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .draw_ellipse_border(x, y, width, height, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_tri(
    handle: *const c_void,
    x1: f32,
    y1: f32,
    x2: f32,
    y2: f32,
    x3: f32,
    y3: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .draw_triangle(x1, y1, x2, y2, x3, y3, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_trib(
    handle: *const c_void,
    x1: f32,
    y1: f32,
    x2: f32,
    y2: f32,
    x3: f32,
    y3: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle)
            .borrow_mut()
            .draw_triangle_border(x1, y1, x2, y2, x3, y3, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_fill(
    handle: *const c_void,
    x: f32,
    y: f32,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle).borrow_mut().flood_fill(x, y, color);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `text` a valid NUL-terminated UTF-8
/// string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_text(
    handle: *const c_void,
    x: f32,
    y: f32,
    text: *const c_char,
    color: u8,
) -> i32 {
    ffi!({
        image_from_handle(handle).borrow_mut().draw_text(
            x,
            y,
            opt_str(text).unwrap_or_default(),
            color,
            None,
        );
        Ok(())
    })
}

/// Blits from `source` onto `handle`. `colkey` uses -1 as the None sentinel,
/// `rotate` / `scale` use NaN.
///
/// # Safety
/// `handle` and `source` must be live handles.
#[no_mangle]
pub unsafe extern "C" fn pyxel_image_blt(
    handle: *const c_void,
    x: f32,
    y: f32,
    source: *const c_void,
    u: f32,
    v: f32,
    width: f32,
    height: f32,
    colkey: i32,
    rotate: f32,
    scale: f32,
) -> i32 {
    ffi!({
        let source = image_from_handle(source).clone();
        image_from_handle(handle).borrow_mut().draw_image(
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
