namespace NetBank;

/// <summary>
/// Represents a monetary amount in the system.
/// Immutable and strongly typed.
/// </summary>
public readonly struct Amount : IEquatable<Amount>, IComparable<Amount>
{
    public long Value { get; }

    public Amount(long value)
    {
        Value = value;
    }
    
    public bool Equals(Amount other) => Value == other.Value;
    public override bool Equals(object obj) => obj is Amount other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public int CompareTo(Amount other) => Value.CompareTo(other.Value);

    public static bool operator ==(Amount left, Amount right) => left.Equals(right);
    public static bool operator !=(Amount left, Amount right) => !(left == right);

    public static bool operator <(Amount left, Amount right) => left.Value < right.Value;
    public static bool operator >(Amount left, Amount right) => left.Value > right.Value;
    public static bool operator <=(Amount left, Amount right) => left.Value <= right.Value;
    public static bool operator >=(Amount left, Amount right) => left.Value >= right.Value;

    public static Amount operator +(Amount left, Amount right) => new Amount(left.Value + right.Value);
    public static Amount operator -(Amount left, Amount right)
    {
        if (left.Value - right.Value < 0)
            throw new InvalidOperationException("Amount cannot be negative.");
        return new Amount(left.Value - right.Value);
    }

    public override string ToString() => Value.ToString();
}