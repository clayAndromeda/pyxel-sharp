//! Opaque-handle FFI for `pyxel::Sound` (the C# `Sound` class).
//!
//! Audio types are `Arc<Mutex<_>>`, so unlike images these handles may be
//! created and dropped from any thread. Component lists (notes/tones/…) are
//! exchanged by copy: `_len` + `_read` into a caller buffer, `_write` from a
//! caller buffer.

use std::ffi::{c_char, c_void};

use pyxel::RcSound;

use crate::{opt_bool, opt_str};

pub(crate) fn sound_into_handle(sound: RcSound) -> *mut c_void {
    Box::into_raw(Box::new(sound)) as *mut c_void
}

/// # Safety
/// `handle` must be a live pointer produced by `sound_into_handle`.
pub(crate) unsafe fn sound_from_handle<'a>(handle: *const c_void) -> &'a RcSound {
    &*(handle as *const RcSound)
}

/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_new(out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        *out_handle = sound_into_handle(pyxel::Sound::new());
        Ok(())
    })
}

/// Returns a handle to the sound bank `pyxel.sounds[index]`.
///
/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_bank(index: u32, out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        let sound = pyxel::sounds()
            .get(index as usize)
            .cloned()
            .ok_or_else(|| format!("sound index out of range: {index}"))?;
        *out_handle = sound_into_handle(sound);
        Ok(())
    })
}

/// Number of sound banks (`pyxel::NUM_SOUNDS`).
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_num_sounds(out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = pyxel::NUM_SOUNDS;
        Ok(())
    })
}

/// Releases the handle's `Arc` clone (safe from any thread).
///
/// # Safety
/// `handle` must be a live handle; it is invalid after this call.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_drop(handle: *mut c_void) -> i32 {
    ffi!({
        drop(Box::from_raw(handle as *mut RcSound));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_len` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_notes_len(handle: *const c_void, out_len: *mut u32) -> i32 {
    ffi!({
        *out_len = audio_lock!(sound_from_handle(handle)).notes.len() as u32;
        Ok(())
    })
}

/// Copies up to `buffer_len` notes into `buffer`.
///
/// # Safety
/// `handle` must be a live handle; `buffer` must hold `buffer_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_notes_read(
    handle: *const c_void,
    buffer: *mut i8,
    buffer_len: u32,
) -> i32 {
    ffi!({
        let sound = audio_lock!(sound_from_handle(handle));
        let count = sound.notes.len().min(buffer_len as usize);
        std::ptr::copy_nonoverlapping(sound.notes.as_ptr(), buffer, count);
        Ok(())
    })
}

/// Replaces the note list with `data_len` values from `data`.
///
/// # Safety
/// `handle` must be a live handle; `data` must hold `data_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_notes_write(
    handle: *const c_void,
    data: *const i8,
    data_len: u32,
) -> i32 {
    ffi!({
        audio_lock!(sound_from_handle(handle)).notes =
            std::slice::from_raw_parts(data, data_len as usize).to_vec();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_len` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_tones_len(handle: *const c_void, out_len: *mut u32) -> i32 {
    ffi!({
        *out_len = audio_lock!(sound_from_handle(handle)).tones.len() as u32;
        Ok(())
    })
}

/// Copies up to `buffer_len` tone indices into `buffer`.
///
/// # Safety
/// `handle` must be a live handle; `buffer` must hold `buffer_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_tones_read(
    handle: *const c_void,
    buffer: *mut u8,
    buffer_len: u32,
) -> i32 {
    ffi!({
        let sound = audio_lock!(sound_from_handle(handle));
        let count = sound.tones.len().min(buffer_len as usize);
        std::ptr::copy_nonoverlapping(sound.tones.as_ptr(), buffer, count);
        Ok(())
    })
}

/// Replaces the tone list with `data_len` values from `data`.
///
/// # Safety
/// `handle` must be a live handle; `data` must hold `data_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_tones_write(
    handle: *const c_void,
    data: *const u8,
    data_len: u32,
) -> i32 {
    ffi!({
        audio_lock!(sound_from_handle(handle)).tones =
            std::slice::from_raw_parts(data, data_len as usize).to_vec();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_len` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_volumes_len(handle: *const c_void, out_len: *mut u32) -> i32 {
    ffi!({
        *out_len = audio_lock!(sound_from_handle(handle)).volumes.len() as u32;
        Ok(())
    })
}

/// Copies up to `buffer_len` volumes into `buffer`.
///
/// # Safety
/// `handle` must be a live handle; `buffer` must hold `buffer_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_volumes_read(
    handle: *const c_void,
    buffer: *mut u8,
    buffer_len: u32,
) -> i32 {
    ffi!({
        let sound = audio_lock!(sound_from_handle(handle));
        let count = sound.volumes.len().min(buffer_len as usize);
        std::ptr::copy_nonoverlapping(sound.volumes.as_ptr(), buffer, count);
        Ok(())
    })
}

/// Replaces the volume list with `data_len` values from `data`.
///
/// # Safety
/// `handle` must be a live handle; `data` must hold `data_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_volumes_write(
    handle: *const c_void,
    data: *const u8,
    data_len: u32,
) -> i32 {
    ffi!({
        audio_lock!(sound_from_handle(handle)).volumes =
            std::slice::from_raw_parts(data, data_len as usize).to_vec();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_len` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_effects_len(handle: *const c_void, out_len: *mut u32) -> i32 {
    ffi!({
        *out_len = audio_lock!(sound_from_handle(handle)).effects.len() as u32;
        Ok(())
    })
}

/// Copies up to `buffer_len` effects into `buffer`.
///
/// # Safety
/// `handle` must be a live handle; `buffer` must hold `buffer_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_effects_read(
    handle: *const c_void,
    buffer: *mut u8,
    buffer_len: u32,
) -> i32 {
    ffi!({
        let sound = audio_lock!(sound_from_handle(handle));
        let count = sound.effects.len().min(buffer_len as usize);
        std::ptr::copy_nonoverlapping(sound.effects.as_ptr(), buffer, count);
        Ok(())
    })
}

/// Replaces the effect list with `data_len` values from `data`.
///
/// # Safety
/// `handle` must be a live handle; `data` must hold `data_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_effects_write(
    handle: *const c_void,
    data: *const u8,
    data_len: u32,
) -> i32 {
    ffi!({
        audio_lock!(sound_from_handle(handle)).effects =
            std::slice::from_raw_parts(data, data_len as usize).to_vec();
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_speed(handle: *const c_void, out_value: *mut u16) -> i32 {
    ffi!({
        *out_value = audio_lock!(sound_from_handle(handle)).speed;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_set_speed(handle: *const c_void, speed: u16) -> i32 {
    ffi!({
        pyxel::Sound::validate_speed(speed)?;
        audio_lock!(sound_from_handle(handle)).speed = speed;
        Ok(())
    })
}

/// Sets all components from their string notations (Python: `sound.set`).
///
/// # Safety
/// `handle` must be a live handle; the strings must be valid NUL-terminated
/// UTF-8.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_set(
    handle: *const c_void,
    notes: *const c_char,
    tones: *const c_char,
    volumes: *const c_char,
    effects: *const c_char,
    speed: u16,
) -> i32 {
    ffi!({
        pyxel::Sound::validate_speed(speed)?;
        audio_lock!(sound_from_handle(handle)).set(
            opt_str(notes).unwrap_or_default(),
            opt_str(tones).unwrap_or_default(),
            opt_str(volumes).unwrap_or_default(),
            opt_str(effects).unwrap_or_default(),
            speed,
        )?;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `notes` valid NUL-terminated UTF-8.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_set_notes(
    handle: *const c_void,
    notes: *const c_char,
) -> i32 {
    ffi!({
        audio_lock!(sound_from_handle(handle)).set_notes(opt_str(notes).unwrap_or_default())?;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `tones` valid NUL-terminated UTF-8.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_set_tones(
    handle: *const c_void,
    tones: *const c_char,
) -> i32 {
    ffi!({
        audio_lock!(sound_from_handle(handle)).set_tones(opt_str(tones).unwrap_or_default())?;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `volumes` valid NUL-terminated UTF-8.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_set_volumes(
    handle: *const c_void,
    volumes: *const c_char,
) -> i32 {
    ffi!({
        audio_lock!(sound_from_handle(handle)).set_volumes(opt_str(volumes).unwrap_or_default())?;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `effects` valid NUL-terminated UTF-8.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_set_effects(
    handle: *const c_void,
    effects: *const c_char,
) -> i32 {
    ffi!({
        audio_lock!(sound_from_handle(handle)).set_effects(opt_str(effects).unwrap_or_default())?;
        Ok(())
    })
}

/// Sets the MML code, or clears it when `code` is null (Python: `sound.mml`).
///
/// # Safety
/// `handle` must be a live handle; `code` null or valid NUL-terminated UTF-8.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_mml(handle: *const c_void, code: *const c_char) -> i32 {
    ffi!({
        let mut sound = audio_lock!(sound_from_handle(handle));
        match opt_str(code) {
            Some(code) => sound.set_mml(code)?,
            None => sound.clear_mml(),
        }
        Ok(())
    })
}

/// Loads a PCM file, or clears the PCM data when `filename` is null
/// (Python: `sound.pcm`).
///
/// # Safety
/// `handle` must be a live handle; `filename` null or valid NUL-terminated
/// UTF-8.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_pcm(handle: *const c_void, filename: *const c_char) -> i32 {
    ffi!({
        let mut sound = audio_lock!(sound_from_handle(handle));
        match opt_str(filename) {
            Some(filename) => sound.load_pcm(filename)?,
            None => sound.clear_pcm(),
        }
        Ok(())
    })
}

/// Renders `sec` seconds to a WAV file (Python: `sound.save`).
///
/// # Safety
/// `handle` must be a live handle and `filename` valid NUL-terminated UTF-8.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_save(
    handle: *const c_void,
    filename: *const c_char,
    sec: f32,
    ffmpeg: i32,
) -> i32 {
    ffi!({
        let sound = audio_lock!(sound_from_handle(handle)).clone();
        sound.save(opt_str(filename).unwrap_or_default(), sec, opt_bool(ffmpeg))?;
        Ok(())
    })
}

/// Playback length in seconds; `out_has_value` is false when unknown
/// (Python: `sound.total_sec`).
///
/// # Safety
/// `handle` must be a live handle; `out_value` and `out_has_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sound_total_sec(
    handle: *const c_void,
    out_value: *mut f32,
    out_has_value: *mut bool,
) -> i32 {
    ffi!({
        let total = audio_lock!(sound_from_handle(handle)).total_seconds();
        *out_has_value = total.is_some();
        *out_value = total.unwrap_or(0.0);
        Ok(())
    })
}
