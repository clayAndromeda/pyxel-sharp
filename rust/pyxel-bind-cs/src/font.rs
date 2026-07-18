//! Opaque-handle FFI for `pyxel::Font` (the C# `Font` class).

use std::ffi::{c_char, c_void};

use pyxel::RcFont;

use crate::{opt_f32, opt_str};

fn font_into_handle(font: RcFont) -> *mut c_void {
    Box::into_raw(Box::new(font)) as *mut c_void
}

/// # Safety
/// `handle` must be a live pointer produced by `font_into_handle`.
pub(crate) unsafe fn font_from_handle<'a>(handle: *const c_void) -> &'a RcFont {
    &*(handle as *const RcFont)
}

/// # Safety
/// `handle` must be null (no font) or a live font handle.
pub(crate) unsafe fn opt_font<'a>(handle: *const c_void) -> Option<&'a RcFont> {
    (!handle.is_null()).then(|| font_from_handle(handle))
}

/// Loads a BDF or TTF font; `font_size` (NaN = default) applies to TTF.
///
/// # Safety
/// `filename` must be a valid NUL-terminated UTF-8 string; `out_handle` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_font_new(
    filename: *const c_char,
    font_size: f32,
    out_handle: *mut *mut c_void,
) -> i32 {
    ffi!({
        let font = pyxel::Font::new(opt_str(filename).unwrap_or_default(), opt_f32(font_size))?;
        *out_handle = font_into_handle(font);
        Ok(())
    })
}

/// Releases the handle's `Rc` clone. Must be called on the pyxel thread.
///
/// # Safety
/// `handle` must be a live handle; it is invalid after this call.
#[no_mangle]
pub unsafe extern "C" fn pyxel_font_drop(handle: *mut c_void) -> i32 {
    ffi!({
        drop(Box::from_raw(handle as *mut RcFont));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle; `text` a valid NUL-terminated UTF-8
/// string; `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_font_text_width(
    handle: *const c_void,
    text: *const c_char,
    out_value: *mut i32,
) -> i32 {
    ffi!({
        *out_value = font_from_handle(handle)
            .borrow_mut()
            .text_width(opt_str(text).unwrap_or_default());
        Ok(())
    })
}
