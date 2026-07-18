//! Math FFI (Python: `pyxel.rndi`, `pyxel.noise`, …). `clamp` and `sgn` are
//! not bound; C# has `Math.Clamp` / `Math.Sign`.

use pyxel::Pyxel;

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_ceil(x: f32, out_value: *mut i32) -> i32 {
    ffi!({
        *out_value = Pyxel::ceil(x);
        Ok(())
    })
}

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_floor(x: f32, out_value: *mut i32) -> i32 {
    ffi!({
        *out_value = Pyxel::floor(x);
        Ok(())
    })
}

/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sqrt(x: f32, out_value: *mut f32) -> i32 {
    ffi!({
        *out_value = Pyxel::sqrt(x);
        Ok(())
    })
}

/// Sine of an angle in degrees.
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_sin(deg: f32, out_value: *mut f32) -> i32 {
    ffi!({
        *out_value = Pyxel::sin(deg);
        Ok(())
    })
}

/// Cosine of an angle in degrees.
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_cos(deg: f32, out_value: *mut f32) -> i32 {
    ffi!({
        *out_value = Pyxel::cos(deg);
        Ok(())
    })
}

/// Arctangent of y/x in degrees.
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_atan2(y: f32, x: f32, out_value: *mut f32) -> i32 {
    ffi!({
        *out_value = Pyxel::atan2(y, x);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_rseed(seed: u32) -> i32 {
    ffi!({
        Pyxel::random_seed(seed);
        Ok(())
    })
}

/// Random integer in [a, b].
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_rndi(a: i32, b: i32, out_value: *mut i32) -> i32 {
    ffi!({
        *out_value = Pyxel::random_int(a, b);
        Ok(())
    })
}

/// Random float in [a, b].
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_rndf(a: f32, b: f32, out_value: *mut f32) -> i32 {
    ffi!({
        *out_value = Pyxel::random_float(a, b);
        Ok(())
    })
}

#[no_mangle]
pub extern "C" fn pyxel_nseed(seed: u32) -> i32 {
    ffi!({
        Pyxel::noise_seed(seed);
        Ok(())
    })
}

/// Perlin noise at (x, y, z).
///
/// # Safety
/// `out_value` must point to writable memory.
#[no_mangle]
pub unsafe extern "C" fn pyxel_noise(x: f32, y: f32, z: f32, out_value: *mut f32) -> i32 {
    ffi!({
        *out_value = Pyxel::noise(x, y, z);
        Ok(())
    })
}
