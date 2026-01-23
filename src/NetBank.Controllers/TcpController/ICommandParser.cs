namespace NetBank.Controllers.TcpController;

public interface ICommandParser
{
    object ParseCommandToDTO(string command);
}