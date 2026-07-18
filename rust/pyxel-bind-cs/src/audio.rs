//! Global playback FFI (Python: `pyxel.play`, `pyxel.playm`, …).
//! Sound-form dispatch (index list / Sound handles / MML) happens in C#.

use std::ffi::{c_char, c_void};

use pyxel::AudioLock;

use crate::sound::sound_from_handle;
use crate::{opt_f32, opt_str};

fn validate_channel(ch: u32) -> Result<(), String> {
    if (ch as usize) < pyxel::channels().len() {
        Ok(())
    } else {
        Err(format!("channel index out of range: {ch}"))
    }
}

/// Plays sound bank indices on channel `ch` (Python: `pyxel.play`).
/// `sec` uses NaN as the None sentinel.
///
/// # Safety
/// `sounds` must point to `sounds_len` u32 values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_play(
    ch: u32,
    sounds: *const u32,
    sounds_len: u32,
    sec: f32,
    should_loop: bool,
    resume: bool,
) -> i32 {
    ffi!({
        let sec = opt_f32(sec);
        pyxel::Channel::validate_sec(sec)?;
        validate_channel(ch)?;
        let sounds = std::slice::from_raw_parts(sounds, sounds_len as usize).to_vec();
        let num_sounds = pyxel::sounds().len();
        for &sound in &sounds {
            if sound as usize >= num_sounds {
                return Err(format!("sound index out of range: {sound}"));
            }
        }
        pyxel::pyxel().play(ch, &sounds, sec, should_loop, resume)?;
        Ok(())
    })
}

/// Plays sound handles on channel `ch` (Python: `pyxel.play` with Sound
/// objects). `sec` uses NaN as the None sentinel.
///
/// # Safety
/// `sounds` must point to `sounds_len` live sound handles.
#[no_mangle]
pub unsafe extern "C" fn pyxel_play_handles(
    ch: u32,
    sounds: *const *const c_void,
    sounds_len: u32,
    sec: f32,
    should_loop: bool,
    resume: bool,
) -> i32 {
    ffi!({
        let sec = opt_f32(sec);
        pyxel::Channel::validate_sec(sec)?;
        validate_channel(ch)?;
        let sounds = (0..sounds_len as usize)
            .map(|i| sound_from_handle(*sounds.add(i)).clone())
            .collect();
        let channel = pyxel::channels()[ch as usize].clone();
        let _lock = AudioLock::lock();
        audio_lock!(channel).play(sounds, sec, should_loop, resume)?;
        Ok(())
    })
}

/// Plays MML code on channel `ch` (Python: `pyxel.play` with a string).
/// `sec` uses NaN as the None sentinel.
///
/// # Safety
/// `code` must be a valid NUL-terminated UTF-8 string.
#[no_mangle]
pub unsafe extern "C" fn pyxel_play_mml(
    ch: u32,
    code: *const c_char,
    sec: f32,
    should_loop: bool,
    resume: bool,
) -> i32 {
    ffi!({
        let sec = opt_f32(sec);
        pyxel::Channel::validate_sec(sec)?;
        validate_channel(ch)?;
        pyxel::pyxel().play_mml(
            ch,
            opt_str(code).unwrap_or_default(),
            sec,
            should_loop,
            resume,
        )?;
        Ok(())
    })
}

/// Plays music bank `msc` (Python: `pyxel.playm`). `sec` uses NaN as the
/// None sentinel.
#[no_mangle]
pub extern "C" fn pyxel_playm(msc: u32, sec: f32, should_loop: bool) -> i32 {
    ffi!({
        let sec = opt_f32(sec);
        pyxel::Channel::validate_sec(sec)?;
        if msc as usize >= pyxel::musics().len() {
            return Err(format!("music index out of range: {msc}"));
        }
        pyxel::pyxel().play_music(msc, sec, should_loop)?;
        Ok(())
    })
}

/// Stops channel `ch`, or every channel when `ch` is negative
/// (Python: `pyxel.stop`).
#[no_mangle]
pub extern "C" fn pyxel_stop(ch: i32) -> i32 {
    ffi!({
        if ch < 0 {
            pyxel::pyxel().stop_all_channels();
        } else {
            validate_channel(ch as u32)?;
            pyxel::pyxel().stop_channel(ch as u32);
        }
        Ok(())
    })
}

/// Current (sound index, seconds) of channel `ch`; `out_has_value` is false
/// when nothing is playing (Python: `pyxel.play_pos`).
///
/// # Safety
/// The out pointers must be writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_play_pos(
    ch: u32,
    out_sound_index: *mut u32,
    out_sec: *mut f32,
    out_has_value: *mut bool,
) -> i32 {
    ffi!({
        validate_channel(ch)?;
        let position = pyxel::pyxel().play_position(ch);
        *out_has_value = position.is_some();
        let (sound_index, sec) = position.unwrap_or((0, 0.0));
        *out_sound_index = sound_index;
        *out_sec = sec;
        Ok(())
    })
}
