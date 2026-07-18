namespace PyxelSharp;

/// <summary>Thrown when a pyxel-core call reports an error.</summary>
public sealed class PyxelException : Exception
{
    public PyxelException(string message) : base(message)
    {
    }
}
