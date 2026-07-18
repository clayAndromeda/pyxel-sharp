namespace PyxelSharp;

/// <summary>
/// A palette color index. Wraps pyxel's <c>Color = u8</c> with the default
/// 16-color palette exposed as named constants. Implicitly convertible from
/// <see cref="int"/> so <c>Pyxel.Cls(0)</c> also works.
/// </summary>
public readonly struct Color : IEquatable<Color>
{
    public byte Value { get; }

    public Color(byte value) => Value = value;

    public static implicit operator Color(int value) => new(checked((byte)value));
    public static implicit operator byte(Color color) => color.Value;

    // Default palette (settings.rs COLOR_*)
    public static readonly Color Black = 0;
    public static readonly Color Navy = 1;
    public static readonly Color Purple = 2;
    public static readonly Color Green = 3;
    public static readonly Color Brown = 4;
    public static readonly Color DarkBlue = 5;
    public static readonly Color LightBlue = 6;
    public static readonly Color White = 7;
    public static readonly Color Red = 8;
    public static readonly Color Orange = 9;
    public static readonly Color Yellow = 10;
    public static readonly Color Lime = 11;
    public static readonly Color Cyan = 12;
    public static readonly Color Gray = 13;
    public static readonly Color Pink = 14;
    public static readonly Color Peach = 15;

    public bool Equals(Color other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Color other && Equals(other);
    public override int GetHashCode() => Value;
    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !left.Equals(right);
    public override string ToString() => $"Color({Value})";
}
