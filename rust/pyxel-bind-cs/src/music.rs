//! Opaque-handle FFI for `pyxel::Music` (the C# `Music` class).
//!
//! The `seqs` (per-channel sound sequences) are exchanged by copy, one
//! sequence at a time; replacing them wholesale is `_clear` + `_append`.

use std::ffi::{c_char, c_void};

use pyxel::RcMusic;

use crate::{opt_bool, opt_str};

pub(crate) fn music_into_handle(music: RcMusic) -> *mut c_void {
    Box::into_raw(Box::new(music)) as *mut c_void
}

/// # Safety
/// `handle` must be a live pointer produced by `music_into_handle`.
pub(crate) unsafe fn music_from_handle<'a>(handle: *const c_void) -> &'a RcMusic {
    &*(handle as *const RcMusic)
}

/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_music_new(out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        *out_handle = music_into_handle(pyxel::Music::new());
        Ok(())
    })
}

/// Returns a handle to the music bank `pyxel.musics[index]`.
///
/// # Safety
/// `out_handle` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_music_bank(index: u32, out_handle: *mut *mut c_void) -> i32 {
    ffi!({
        let music = pyxel::musics()
            .get(index as usize)
            .cloned()
            .ok_or_else(|| format!("music index out of range: {index}"))?;
        *out_handle = music_into_handle(music);
        Ok(())
    })
}

/// Number of music banks (`pyxel::NUM_MUSICS`).
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_num_musics(out_value: *mut u32) -> i32 {
    ffi!({
        *out_value = pyxel::NUM_MUSICS;
        Ok(())
    })
}

/// Releases the handle's `Arc` clone (safe from any thread).
///
/// # Safety
/// `handle` must be a live handle; it is invalid after this call.
#[no_mangle]
pub unsafe extern "C" fn pyxel_music_drop(handle: *mut c_void) -> i32 {
    ffi!({
        drop(Box::from_raw(handle as *mut RcMusic));
        Ok(())
    })
}

/// Number of sequences (Python: `len(music.seqs)`).
///
/// # Safety
/// `handle` must be a live handle and `out_len` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_music_seqs_len(handle: *const c_void, out_len: *mut u32) -> i32 {
    ffi!({
        *out_len = audio_lock!(music_from_handle(handle)).seqs.len() as u32;
        Ok(())
    })
}

/// Length of sequence `index`.
///
/// # Safety
/// `handle` must be a live handle and `out_len` writable.
#[no_mangle]
pub unsafe extern "C" fn pyxel_music_seq_len(
    handle: *const c_void,
    index: u32,
    out_len: *mut u32,
) -> i32 {
    ffi!({
        let music = audio_lock!(music_from_handle(handle));
        let seq = music
            .seqs
            .get(index as usize)
            .ok_or_else(|| format!("seq index out of range: {index}"))?;
        *out_len = seq.len() as u32;
        Ok(())
    })
}

/// Copies up to `buffer_len` sound indices of sequence `index` into `buffer`.
///
/// # Safety
/// `handle` must be a live handle; `buffer` must hold `buffer_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_music_seq_read(
    handle: *const c_void,
    index: u32,
    buffer: *mut u32,
    buffer_len: u32,
) -> i32 {
    ffi!({
        let music = audio_lock!(music_from_handle(handle));
        let seq = music
            .seqs
            .get(index as usize)
            .ok_or_else(|| format!("seq index out of range: {index}"))?;
        let count = seq.len().min(buffer_len as usize);
        std::ptr::copy_nonoverlapping(seq.as_ptr(), buffer, count);
        Ok(())
    })
}

/// Replaces sequence `index` with `data_len` sound indices from `data`.
///
/// # Safety
/// `handle` must be a live handle; `data` must hold `data_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_music_seq_write(
    handle: *const c_void,
    index: u32,
    data: *const u32,
    data_len: u32,
) -> i32 {
    ffi!({
        let mut music = audio_lock!(music_from_handle(handle));
        let seq = music
            .seqs
            .get_mut(index as usize)
            .ok_or_else(|| format!("seq index out of range: {index}"))?;
        *seq = std::slice::from_raw_parts(data, data_len as usize).to_vec();
        Ok(())
    })
}

/// Removes all sequences.
///
/// # Safety
/// `handle` must be a live handle.
#[no_mangle]
pub unsafe extern "C" fn pyxel_music_seqs_clear(handle: *const c_void) -> i32 {
    ffi!({
        audio_lock!(music_from_handle(handle)).seqs.clear();
        Ok(())
    })
}

/// Appends a sequence of `data_len` sound indices.
///
/// # Safety
/// `handle` must be a live handle; `data` must hold `data_len` values.
#[no_mangle]
pub unsafe extern "C" fn pyxel_music_seqs_append(
    handle: *const c_void,
    data: *const u32,
    data_len: u32,
) -> i32 {
    ffi!({
        audio_lock!(music_from_handle(handle))
            .seqs
            .push(std::slice::from_raw_parts(data, data_len as usize).to_vec());
        Ok(())
    })
}

/// Renders `sec` seconds to a WAV file (Python: `music.save`).
///
/// # Safety
/// `handle` must be a live handle and `filename` valid NUL-terminated UTF-8.
#[no_mangle]
pub unsafe extern "C" fn pyxel_music_save(
    handle: *const c_void,
    filename: *const c_char,
    sec: f32,
    ffmpeg: i32,
) -> i32 {
    ffi!({
        let music = audio_lock!(music_from_handle(handle)).clone();
        music.save(opt_str(filename).unwrap_or_default(), sec, opt_bool(ffmpeg))?;
        Ok(())
    })
}
