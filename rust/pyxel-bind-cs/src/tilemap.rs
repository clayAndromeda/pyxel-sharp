//! Opaque-handle FFI for `pyxel::Tilemap` (the C# `Tilemap` class).
//!
//! Handles follow the same `Box<Rc<RefCell<_>>>` scheme as `image.rs`.
//! A tile is a `(u16, u16)` pair of image tile coordinates, passed as two
//! scalars; optional tiles use `i32` coordinates with a negative x as the
//! None sentinel.

use std::ffi::{c_char, c_void};

use pyxel::{ImageSource, RcTilemap, Tile};

use crate::image::image_from_handle;
use crate::{opt_f32, opt_str};

pub(crate) fn tilemap_into_handle(tilemap: RcTilemap) -> *mut c_void {
    Box::into_raw(Box::new(tilemap)) as *mut c_void
}

/// # Safety
/// `handle` must be a live pointer produced by `tilemap_into_handle`.
pub(crate) unsafe fn tilemap_from_handle<'a>(handle: *const c_void) -> &'a RcTilemap {
    &*(handle as *const RcTilemap)
}

/// # Safety
/// `image` must be null (use the bank `index`) or a live image handle.
unsafe fn image_source(image: *const c_void, index: u32) -> ImageSource {
    if image.is_null() {
        ImageSource::Index(index)
    } else {
        ImageSource::Image(image_from_handle(image).clone())
    }
}

fn opt_tile(x: i32, y: i32) -> Option<Tile> {
    (x >= 0).then_some((x as u16, y as u16))
}

/// Creates a tilemap whose tiles reference either the image bank `image_index`
/// (when `image` is null) or the given image handle.
///
/// # Safety
/// `image` must be null or a live image handle; `out_handle` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_new(
    width: u32,
    height: u32,
    image: *const c_void,
    image_index: u32,
    out_handle: *mut *mut c_void,
) -> i32 {
    ffi!({
        let tilemap = pyxel::Tilemap::try_new(width, height, image_source(image, image_index))?;
        *out_handle = tilemap_into_handle(tilemap);
        Ok(())
    })
}

/// # Safety
/// `filename` must be a valid NUL-terminated UTF-8 string; `out_handle` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_from_tmx(
    filename: *const c_char,
    layer: u32,
    out_handle: *mut *mut c_void,
) -> i32 {
    ffi!({
        let tilemap = pyxel::Tilemap::from_tmx(opt_str(filename).unwrap_or_default(), layer)?;
        *out_handle = tilemap_into_handle(tilemap);
        Ok(())
    })
}

/// Returns a handle to the tilemap bank `pyxel.tilemaps[index]`.
///
/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_bank(index: u32, out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        let tilemap = pyxel::tilemaps()
            .get(index as usize)
            .cloned()
            .ok_or_else(|| format!("tilemap index out of range: {index}"))?;
        *out_handle = tilemap_into_handle(tilemap);
        Ok(())
    })
}

/// Number of tilemap banks (`pyxel::NUM_TILEMAPS`).
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_num_tilemaps(out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = pyxel::NUM_TILEMAPS;
        Ok(())
    })
}

/// Releases the handle's `Rc` clone. Must be called on the pyxel thread.
///
/// # Safety
/// `handle` must be a live handle; it is invalid after this call.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_drop(handle: *mut c_void) -> i32 {
    ffi!({
        drop(Box::from_raw(handle as *mut RcTilemap));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_width(handle: *const c_void, out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = tilemap_from_handle(handle).borrow().width();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_height(handle: *const c_void, out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = tilemap_from_handle(handle).borrow().height();
        Ok(())
    })
}

/// Reads the image source: bank index into `out_index` (with `out_image` set
/// to null), or a new image handle into `out_image` (with `out_index` -1).
///
/// # Safety
/// `handle` must be a live handle; `out_index` and `out_image` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_imgsrc(
    handle: *const c_void,
    out_index: *mut i32,
    out_image: *mut *mut c_void,
) -> i32 {
    ffi!({
        match &tilemap_from_handle(handle).borrow().imgsrc {
            ImageSource::Index(index) => {
                *out_index = *index as i32;
                *out_image = std::ptr::null_mut();
            }
            ImageSource::Image(image) => {
                *out_index = -1;
                *out_image = crate::image::image_into_handle(image.clone());
            }
        }
        Ok(())
    })
}

/// Points the tilemap at an image bank or (non-null `image`) an image handle.
///
/// # Safety
/// `handle` must be a live handle; `image` null or a live image handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_set_imgsrc(
    handle: *const c_void,
    image: *const c_void,
    image_index: u32,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle).borrow_mut().imgsrc = image_source(image, image_index);
        Ok(())
    })
}

/// Pointer to the tile buffer (`width * height` tiles, two u16 each).
/// Valid while the tilemap is alive and its size unchanged.
///
/// # Safety
/// `handle` must be a live handle and `out_ptr` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_data_ptr(
    handle: *const c_void,
    out_ptr: *mut *mut u16,
) -> i32 {
    ffi!({
        *out_ptr = tilemap_from_handle(handle).borrow_mut().data_ptr() as *mut u16;
        Ok(())
    })
}

/// Writes rows of tile data at (x, y), like Python's `tilemap.set`.
///
/// # Safety
/// `handle` must be a live handle; `data` must point to `data_len` valid
/// NUL-terminated UTF-8 strings.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_set(
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
        tilemap_from_handle(handle).borrow_mut().set(x, y, &rows)?;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `filename` a valid NUL-terminated
/// UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_load(
    handle: *const c_void,
    x: i32,
    y: i32,
    filename: *const c_char,
    layer: u32,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle).borrow_mut().load(
            x,
            y,
            opt_str(filename).unwrap_or_default(),
            layer,
        )?;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_clip(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .set_clip_rect(x, y, width, height);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_clip_reset(handle: *const c_void) -> i32 {
    ffi!({
        tilemap_from_handle(handle).borrow_mut().reset_clip_rect();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_camera(handle: *const c_void, x: f32, y: f32) -> i32 {
    ffi!({
        tilemap_from_handle(handle).borrow_mut().set_camera(x, y);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_camera_reset(handle: *const c_void) -> i32 {
    ffi!({
        tilemap_from_handle(handle).borrow_mut().reset_camera();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_cls(handle: *const c_void, tile_x: u16, tile_y: u16) -> i32 {
    ffi!({
        tilemap_from_handle(handle).borrow_mut().clear((tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle; `out_tile_x` and `out_tile_y` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_pget(
    handle: *const c_void,
    x: f32,
    y: f32,
    out_tile_x: *mut u16,
    out_tile_y: *mut u16,
) -> i32 {
    ffi!({
        let (tile_x, tile_y) = tilemap_from_handle(handle).borrow().tile(x, y);
        *out_tile_x = tile_x;
        *out_tile_y = tile_y;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_pset(
    handle: *const c_void,
    x: f32,
    y: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .set_tile(x, y, (tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_line(
    handle: *const c_void,
    x1: f32,
    y1: f32,
    x2: f32,
    y2: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .draw_line(x1, y1, x2, y2, (tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_rect(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .draw_rect(x, y, width, height, (tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_rectb(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .draw_rect_border(x, y, width, height, (tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_circ(
    handle: *const c_void,
    x: f32,
    y: f32,
    radius: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .draw_circle(x, y, radius, (tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_circb(
    handle: *const c_void,
    x: f32,
    y: f32,
    radius: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .draw_circle_border(x, y, radius, (tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_elli(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .draw_ellipse(x, y, width, height, (tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_ellib(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .draw_ellipse_border(x, y, width, height, (tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_tri(
    handle: *const c_void,
    x1: f32,
    y1: f32,
    x2: f32,
    y2: f32,
    x3: f32,
    y3: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .draw_triangle(x1, y1, x2, y2, x3, y3, (tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_trib(
    handle: *const c_void,
    x1: f32,
    y1: f32,
    x2: f32,
    y2: f32,
    x3: f32,
    y3: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .draw_triangle_border(x1, y1, x2, y2, x3, y3, (tile_x, tile_y));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_fill(
    handle: *const c_void,
    x: f32,
    y: f32,
    tile_x: u16,
    tile_y: u16,
) -> i32 {
    ffi!({
        tilemap_from_handle(handle)
            .borrow_mut()
            .flood_fill(x, y, (tile_x, tile_y));
        Ok(())
    })
}

/// Slides a rect by (dx, dy) against wall tiles, returning the allowed
/// movement (Python: `tilemap.collide`). `walls` is `walls_len` tiles laid
/// out as u16 pairs.
///
/// # Safety
/// `handle` must be a live handle; `walls` must point to `walls_len * 2`
/// u16 values; `out_dx` and `out_dy` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_collide(
    handle: *const c_void,
    x: f32,
    y: f32,
    width: f32,
    height: f32,
    dx: f32,
    dy: f32,
    walls: *const u16,
    walls_len: u32,
    out_dx: *mut f32,
    out_dy: *mut f32,
) -> i32 {
    ffi!({
        let walls: Vec<Tile> = (0..walls_len as usize)
            .map(|i| (*walls.add(i * 2), *walls.add(i * 2 + 1)))
            .collect();
        let (allowed_dx, allowed_dy) = tilemap_from_handle(handle)
            .borrow()
            .collide(x, y, width, height, dx, dy, &walls);
        *out_dx = allowed_dx;
        *out_dy = allowed_dy;
        Ok(())
    })
}

/// Copies a region of another tilemap onto this one. `tilekey` uses a
/// negative `tilekey_x` as the None sentinel; `rotate` / `scale` use NaN.
///
/// # Safety
/// `handle` and `source` must be live tilemap handles.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tilemap_blt(
    handle: *const c_void,
    x: f32,
    y: f32,
    source: *const c_void,
    u: f32,
    v: f32,
    width: f32,
    height: f32,
    tilekey_x: i32,
    tilekey_y: i32,
    rotate: f32,
    scale: f32,
) -> i32 {
    ffi!({
        let source = tilemap_from_handle(source).clone();
        tilemap_from_handle(handle).borrow_mut().draw_tilemap(
            x,
            y,
            &source,
            u,
            v,
            width,
            height,
            opt_tile(tilekey_x, tilekey_y),
            opt_f32(rotate),
            opt_f32(scale),
        );
        Ok(())
    })
}
