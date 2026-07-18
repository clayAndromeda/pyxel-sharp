//! Opaque-handle FFI for `pyxel::Tone` (the C# `Tone` class).
//! The deprecated `noise` / `waveform` accessors are not bound.

use std::ffi::c_void;

use pyxel::RcTone;

pub(crate) fn tone_into_handle(tone: RcTone) -> *mut c_void {
    Box::into_raw(Box::new(tone)) as *mut c_void
}

/// # Safety
/// `handle` must be a live pointer produced by `tone_into_handle`.
pub(crate) unsafe fn tone_from_handle<'a>(handle: *const c_void) -> &'a RcTone {
    &*(handle as *const RcTone)
}

/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_new(out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        *out_handle = tone_into_handle(pyxel::Tone::new());
        Ok(())
    })
}

/// Returns a handle to the tone bank `pyxel.tones[index]`.
///
/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_bank(index: u32, out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        let tone = pyxel::tones()
            .get(index as usize)
            .cloned()
            .ok_or_else(|| format!("tone index out of range: {index}"))?;
        *out_handle = tone_into_handle(tone);
        Ok(())
    })
}

/// Number of tone banks (`pyxel::NUM_TONES`).
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_num_tones(out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = pyxel::NUM_TONES;
        Ok(())
    })
}

/// Releases the handle's `Arc` clone (safe from any thread).
///
/// # Safety
/// `handle` must be a live handle; it is invalid after this call.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_drop(handle: *mut c_void) -> i32 {
    ffi!({
        drop(Box::from_raw(handle as *mut RcTone));
        Ok(())
    })
}

/// Tone mode (0 = wavetable, 1 = short-period noise, 2 = long-period noise).
///
/// # Safety
/// `handle` must be a live handle and `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_mode(handle: *const c_void, out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = audio_lock!(tone_from_handle(handle)).mode.into();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_set_mode(handle: *const c_void, mode: u32) -> i32 {
    ffi!({
        audio_lock!(tone_from_handle(handle)).mode = pyxel::ToneMode::from(mode);
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_sample_bits(
    handle: *const c_void,
    out_value: *mut u32,
) -> i32 {
    ffi!({
        *out_value = audio_lock!(tone_from_handle(handle)).sample_bits;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_set_sample_bits(
    handle: *const c_void,
    sample_bits: u32,
) -> i32 {
    ffi!({
        if !(1..=pyxel::AUDIO_SAMPLE_BITS).contains(&sample_bits) {
            return Err(format!(
                "sample_bits must be between 1 and {}",
                pyxel::AUDIO_SAMPLE_BITS
            ));
        }
        audio_lock!(tone_from_handle(handle)).sample_bits = sample_bits;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_gain(handle: *const c_void, out_value: *mut f32) -> i32 {
    ffi!({
        *out_value = audio_lock!(tone_from_handle(handle)).gain;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_set_gain(handle: *const c_void, gain: f32) -> i32 {
    ffi!({
        audio_lock!(tone_from_handle(handle)).gain = gain;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_len` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_wavetable_len(
    handle: *const c_void,
    out_len: *mut u32,
) -> i32 {
    ffi!({
        *out_len = audio_lock!(tone_from_handle(handle)).wavetable.len() as u32;
        Ok(())
    })
}

/// Copies up to `buffer_len` wavetable samples into `buffer`.
///
/// # Safety
/// `handle` must be a live handle; `buffer` must hold `buffer_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_wavetable_read(
    handle: *const c_void,
    buffer: *mut u32,
    buffer_len: u32,
) -> i32 {
    ffi!({
        let tone = audio_lock!(tone_from_handle(handle));
        let count = tone.wavetable.len().min(buffer_len as usize);
        std::ptr::copy_nonoverlapping(tone.wavetable.as_ptr(), buffer, count);
        Ok(())
    })
}

/// Replaces the wavetable with `data_len` samples from `data`.
///
/// # Safety
/// `handle` must be a live handle; `data` must hold `data_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_tone_wavetable_write(
    handle: *const c_void,
    data: *const u32,
    data_len: u32,
) -> i32 {
    ffi!({
        audio_lock!(tone_from_handle(handle)).wavetable =
            std::slice::from_raw_parts(data, data_len as usize).to_vec();
        Ok(())
    })
}
