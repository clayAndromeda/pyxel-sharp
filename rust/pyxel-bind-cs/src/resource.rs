//! Resource FFI: .pyxres load/save, palettes, captures, and user data dir.

use std::cell::RefCell;
use std::ffi::{c_char, CString};

use crate::{opt_bool, opt_str, opt_u32};

thread_local! {
    // Backing storage for string results (pyxel_user_data_dir); the returned
    // pointer stays valid until the next string-returning call on this thread.
    static STRING_RESULT: RefCell<CString> = RefCell::new(CString::default());
}

/// Loads a .pyxres resource file. The `exclude_*` flags are `i32` opt-bools
/// (-1 = None).
///
/// # Safety
/// `filename` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_load(
    filename: *const c_char,
    exclude_images: i32,
    exclude_tilemaps: i32,
    exclude_sounds: i32,
    exclude_musics: i32,
) -> i32 {
    ffi!({
        pyxel::pyxel().load_resource(
            opt_str(filename).unwrap_or_default(),
            opt_bool(exclude_images),
            opt_bool(exclude_tilemaps),
            opt_bool(exclude_sounds),
            opt_bool(exclude_musics),
        )?;
        Ok(())
    })
}

/// Saves a .pyxres resource file. The `exclude_*` flags are `i32` opt-bools
/// (-1 = None).
///
/// # Safety
/// `filename` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_save(
    filename: *const c_char,
    exclude_images: i32,
    exclude_tilemaps: i32,
    exclude_sounds: i32,
    exclude_musics: i32,
) -> i32 {
    ffi!({
        pyxel::pyxel().save_resource(
            opt_str(filename).unwrap_or_default(),
            opt_bool(exclude_images),
            opt_bool(exclude_tilemaps),
            opt_bool(exclude_sounds),
            opt_bool(exclude_musics),
        )?;
        Ok(())
    })
}

/// # Safety
/// `filename` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_load_pal(filename: *const c_char) -> i32 {
    ffi!({
        pyxel::pyxel().load_palette(opt_str(filename).unwrap_or_default())?;
        Ok(())
    })
}

/// # Safety
/// `filename` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_save_pal(filename: *const c_char) -> i32 {
    ffi!({
        pyxel::pyxel().save_palette(opt_str(filename).unwrap_or_default())?;
        Ok(())
    })
}

/// Saves a screenshot; a null `filename` uses a timestamped desktop path.
/// `scale` uses `u32::MAX` as the None sentinel.
///
/// # Safety
/// `filename` must be null or a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_screenshot(filename: *const c_char, scale: u32) -> i32 {
    ffi!({
        pyxel::pyxel().save_screenshot(opt_str(filename), opt_u32(scale))?;
        Ok(())
    })
}

/// Saves the recent frames as a GIF; a null `filename` uses a timestamped
/// desktop path. `scale` uses `u32::MAX` as the None sentinel.
///
/// # Safety
/// `filename` must be null or a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_screencast(filename: *const c_char, scale: u32) -> i32 {
    ffi!({
        pyxel::pyxel().save_screencast(opt_str(filename), opt_u32(scale))?;
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_reset_screencast() -> i32 {
    ffi!({
        pyxel::pyxel().reset_screencast();
        Ok(())
    })
}

/// Returns the per-user data directory for the app. The pointer written to
/// `out_path` stays valid until the next string-returning call on this thread.
///
/// # Safety
/// `vendor_name` and `app_name` must be valid NUL-terminated UTF-8 strings;
/// `out_path` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_user_data_dir(
    vendor_name: *const c_char,
    app_name: *const c_char,
    out_path: *mut *const c_char,
) -> i32 {
    ffi!({
        let path = pyxel::pyxel().user_data_dir(
            opt_str(vendor_name).unwrap_or_default(),
            opt_str(app_name).unwrap_or_default(),
        )?;
        let c_path = CString::new(path).unwrap_or_default();
        STRING_RESULT.with(|slot| {
            *slot.borrow_mut() = c_path;
            *out_path = slot.borrow().as_ptr();
        });
        Ok(())
    })
}
