namespace NetBank.Controllers.TcpController.Commands;

public interface ICommandFactory
{
    public ICommand Create(object commandRecord);
}