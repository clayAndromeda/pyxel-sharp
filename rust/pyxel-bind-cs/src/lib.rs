//! C# binding layer for pyxel-core.
//!
//! This crate is the C# counterpart of `crates/pyxel-binding` (the PyO3 layer):
//! a thin `extern "C"` facade over the pyxel-core singleton. Conventions:
//! - functions are named `pyxel_` + the Python API name
//! - `Option<u32>` parameters use `u32::MAX` as the None sentinel
//! - `Option<f32>` parameters use NaN, `Option<Color>` uses `i32` (-1 = None)
//! - optional strings are null pointers, optional bools are `i32` (-1 = None)
//! - every fallible or panicking call returns `i32` (0 = ok) and stores the
//!   message for `pyxel_last_error`; value getters write through out pointers
//! - opaque handles (`Image` etc.) are `Box<Rc<RefCell<T>>>` raw pointers and
//!   must be released with the matching `pyxel_*_drop` on the pyxel thread

/// Wraps an FFI function body so that pyxel-core panics (asserts, RefCell
/// borrow failures, …) become an error return instead of an abort. The body
/// evaluates to `Result<(), String>`.
macro_rules! ffi {
    ($body:expr) => {
        match std::panic::catch_unwind(std::panic::AssertUnwindSafe(
            || -> Result<(), String> { $body },
        )) {
            Ok(Ok(())) => 0,
            Ok(Err(message)) => {
                crate::set_last_error(&message);
                -1
            }
            Err(payload) => {
                crate::set_last_error(&crate::panic_message(payload.as_ref()));
                -1
            }
        }
    };
}

mod graphics;
mod image;
mod input;
mod system;

use std::cell::RefCell;
use std::ffi::{c_char, CStr, CString};

pub(crate) const NONE_U32: u32 = u32::MAX;

pub(crate) fn opt_u32(value: u32) -> Option<u32> {
    (value != NONE_U32).then_some(value)
}

pub(crate) fn opt_f32(value: f32) -> Option<f32> {
    (!value.is_nan()).then_some(value)
}

pub(crate) fn opt_color(value: i32) -> Option<u8> {
    (value >= 0).then_some(value as u8)
}

pub(crate) fn opt_bool(value: i32) -> Option<bool> {
    match value {
        -1 => None,
        0 => Some(false),
        _ => Some(true),
    }
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

pub(crate) fn panic_message(payload: &(dyn std::any::Any + Send)) -> String {
    if let Some(message) = payload.downcast_ref::<&str>() {
        (*message).to_string()
    } else if let Some(message) = payload.downcast_ref::<String>() {
        message.clone()
    } else {
        "panic in pyxel-core".to_string()
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
