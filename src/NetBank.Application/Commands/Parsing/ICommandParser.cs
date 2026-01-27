namespace NetBank.Commands.Parsing;

public interface ICommandParser
{
    object ParseCommandToDTO(string command);
}