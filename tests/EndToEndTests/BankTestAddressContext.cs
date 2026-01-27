namespace EndToEndTests;

public sealed class BankTestAddressContext
{
    public string Address { get; }

    public BankTestAddressContext(string address)
    {
        Address = address;
    }

    public override string ToString()
    {
        return Address;
    }
}