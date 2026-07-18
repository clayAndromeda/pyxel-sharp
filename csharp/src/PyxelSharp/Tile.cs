namespace PyxelSharp;

/// <summary>
/// A tilemap cell: the (x, y) tile coordinates into the tilemap's source
/// image, in <c>TILE_SIZE</c> (8px) units. Mirrors pyxel's
/// <c>Tile = (u16, u16)</c>. Implicitly convertible from a tuple so
/// <c>tilemap.Pset(0, 0, (1, 2))</c> works like Python.
/// </summary>
public readonly struct Tile : IEquatable<Tile>
{
    public ushort X { get; }
    public ushort Y { get; }

    public Tile(int x, int y)
    {
        X = checked((ushort)x);
        Y = checked((ushort)y);
    }

    public static implicit operator Tile((int X, int Y) value) => new(value.X, value.Y);

    public bool Equals(Tile other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is Tile other && Equals(other);
    public override int GetHashCode() => (X << 16) | Y;
    public static bool operator ==(Tile left, Tile right) => left.Equals(right);
    public static bool operator !=(Tile left, Tile right) => !left.Equals(right);
    public override string ToString() => $"Tile({X}, {Y})";
}
