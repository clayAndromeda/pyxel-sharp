//! C# binding layer for pyxel-core.
//!
//! This crate is the C# counterpart of `crates/pyxel-binding` (the PyO3 layer):
//! a thin `extern "C"` facade over the pyxel-core singleton. Conventions:
//! - functions are named `pyxel_` + the Python API name
//! - `Option<u32>` parameters use `u32::MAX` as the None sentinel
//! - optional strings are null pointers, optional bools are `i32` (-1 = None)
//! - fallible functions return `i32` (0 = ok) and store the message for
//!   `pyxel_last_error`

mod graphics;
mod input;
mod system;

use std::cell::RefCell;
use std::ffi::{c_char, CStr, CString};

pub(crate) const NONE_U32: u32 = u32::MAX;

pub(crate) fn opt_u32(value: u32) -> Option<u32> {
    (value != NONE_U32).then_some(value)
}

/// # Safety
/// `ptr` must be null or point to a valid NUL-terminated UTF-8 string.
pub(crate) unsafe fn opt_str<'a>(ptr: *const c_char) -> Option<&'a str> {
    if ptr.is_null() {
        None
    } else {
        Some(CStr::from_ptr(ptr).to_str().expect("invalid UTF-8 string"))
    }
}

thread_local! {
    static LAST_ERROR: RefCell<CString> = RefCell::new(CString::default());
}

pub(crate) fn set_last_error(message: &str) {
    let c_message = CString::new(message).unwrap_or_default();
    LAST_ERROR.with(|slot| *slot.borrow_mut() = c_message);
}

/// Returns the message of the last failed call. The pointer stays valid until
/// the next failing call on the same thread.
#[no_mangle]
pub extern "C" fn pyxel_last_error() -> *const c_char {
    LAST_ERROR.with(|slot| slot.borrow().as_ptr())
}
