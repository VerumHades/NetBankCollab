namespace NetBank.Controllers.TcpController;

public interface ICommandInterpreter
{
    Task<string> ExecuteTextCommand(string commandString);
}