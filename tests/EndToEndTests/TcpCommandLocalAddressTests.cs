namespace EndToEndTests;

public sealed class TcpCommandLocalAddressTests(BankServerFixture fixture)
    : TcpCommandTestsBase(fixture)
{
    protected override string ActiveAddress => fixture.Address;
    protected override int ActivePort => fixture.Port;
}
    