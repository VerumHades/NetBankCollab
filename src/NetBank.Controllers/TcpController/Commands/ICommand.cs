namespace NetBank.Controllers.TcpController.Commands;

public interface ICommand
{
    Task<string> ExecuteAsync();
}