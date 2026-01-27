namespace NetBank.Commands;

public interface ICommandInterpreter
{
    Task<string> ExecuteTextCommand(string commandString);
}