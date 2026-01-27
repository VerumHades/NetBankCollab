namespace EndToEndTests;

public class TcpCommandDelegatedAddressTests(BankServerFixture fixture): TcpCommandTestsBase(fixture)
{
    protected override string ActiveAddress => fixture.TargetAddress;
    protected override int ActivePort => fixture.TargetPort;
}