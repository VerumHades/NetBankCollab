namespace NetBank;

public sealed record AccountIdentifier(int Number)
{
    public override string ToString()
    {
        return $"{Number}";
    }
};
