//! Opaque-handle FFI for `pyxel::Channel` (the C# `Channel` class).
//!
//! Playback dispatch by sound form (bank index / Sound object / MML) is done
//! on the C# side; the FFI only takes sound handle lists or MML strings.
//! Channel state is guarded by both the global `AudioLock` and the object
//! mutex, mirroring pyxel-binding.

use std::ffi::{c_char, c_void};

use pyxel::{AudioLock, RcChannel};

use crate::sound::sound_from_handle;
use crate::{opt_f32, opt_str};

pub(crate) fn channel_into_handle(channel: RcChannel) -> *mut c_void {
    Box::into_raw(Box::new(channel)) as *mut c_void
}

/// # Safety
/// `handle` must be a live pointer produced by `channel_into_handle`.
pub(crate) unsafe fn channel_from_handle<'a>(handle: *const c_void) -> &'a RcChannel {
    &*(handle as *const RcChannel)
}

/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_new(out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        *out_handle = channel_into_handle(pyxel::Channel::new());
        Ok(())
    })
}

/// Returns a handle to the mixer channel `pyxel.channels[index]`.
///
/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_bank(index: u32, out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        let channel = pyxel::channels()
            .get(index as usize)
            .cloned()
            .ok_or_else(|| format!("channel index out of range: {index}"))?;
        *out_handle = channel_into_handle(channel);
        Ok(())
    })
}

/// Number of mixer channels (`pyxel::NUM_CHANNELS`).
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_num_channels(out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = pyxel::NUM_CHANNELS;
        Ok(())
    })
}

/// Releases the handle's `Arc` clone (safe from any thread).
///
/// # Safety
/// `handle` must be a live handle; it is invalid after this call.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_drop(handle: *mut c_void) -> i32 {
    ffi!({
        drop(Box::from_raw(handle as *mut RcChannel));
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_gain(handle: *const c_void, out_value: *mut f32) -> i32 {
    ffi!({
        *out_value = audio_lock!(channel_from_handle(handle)).gain;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_set_gain(handle: *const c_void, gain: f32) -> i32 {
    ffi!({
        audio_lock!(channel_from_handle(handle)).gain = gain;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle and `out_value` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_detune(handle: *const c_void, out_value: *mut i32) -> i32 {
    ffi!({
        *out_value = audio_lock!(channel_from_handle(handle)).detune;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_set_detune(handle: *const c_void, detune: i32) -> i32 {
    ffi!({
        audio_lock!(channel_from_handle(handle)).detune = detune;
        Ok(())
    })
}

/// Plays `sounds_len` sound handles in order (Python: `channel.play`).
/// `sec` uses NaN as the None sentinel.
///
/// # Safety
/// `handle` must be a live channel handle; `sounds` must point to
/// `sounds_len` live sound handles.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_play(
    handle: *const c_void,
    sounds: *const *const c_void,
    sounds_len: u32,
    sec: f32,
    should_loop: bool,
    resume: bool,
) -> i32 {
    ffi!({
        let sec = opt_f32(sec);
        pyxel::Channel::validate_sec(sec)?;
        let sounds = (0..sounds_len as usize)
            .map(|i| sound_from_handle(*sounds.add(i)).clone())
            .collect();
        let _lock = AudioLock::lock();
        audio_lock!(channel_from_handle(handle)).play(sounds, sec, should_loop, resume)?;
        Ok(())
    })
}

/// Plays MML code (Python: `channel.play(mml)`). `sec` uses NaN as the None
/// sentinel.
///
/// # Safety
/// `handle` must be a live channel handle and `code` valid NUL-terminated
/// UTF-8.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_play_mml(
    handle: *const c_void,
    code: *const c_char,
    sec: f32,
    should_loop: bool,
    resume: bool,
) -> i32 {
    ffi!({
        let sec = opt_f32(sec);
        pyxel::Channel::validate_sec(sec)?;
        let _lock = AudioLock::lock();
        audio_lock!(channel_from_handle(handle)).play_mml(
            opt_str(code).unwrap_or_default(),
            sec,
            should_loop,
            resume,
        )?;
        Ok(())
    })
}

/// # Safety
/// `handle` must be a live channel handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_stop(handle: *const c_void) -> i32 {
    ffi!({
        let _lock = AudioLock::lock();
        audio_lock!(channel_from_handle(handle)).stop();
        Ok(())
    })
}

/// Current (sound index, seconds) of playback; `out_has_value` is false when
/// nothing is playing (Python: `channel.play_pos`).
///
/// # Safety
/// `handle` must be a live channel handle; the out pointers writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_channel_play_pos(
    handle: *const c_void,
    out_sound_index: *mut u32,
    out_sec: *mut f32,
    out_has_value: *mut bool,
) -> i32 {
    ffi!({
        let _lock = AudioLock::lock();
        let position = audio_lock!(channel_from_handle(handle)).play_position();
        *out_has_value = position.is_some();
        let (sound_index, sec) = position.unwrap_or((0, 0.0));
        *out_sound_index = sound_index;
        *out_sec = sec;
        Ok(())
    })
}
