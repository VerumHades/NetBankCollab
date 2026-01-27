namespace NetBank.Commands;

public interface ICommandDelegator
{
    Task<string> DelegateTextCommand(string commandString, string address);
    bool ShouldBeDelegated(string address);
}