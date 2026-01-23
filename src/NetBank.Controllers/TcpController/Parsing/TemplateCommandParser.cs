using System.Reflection;

namespace NetBank.Controllers.TcpController.Parsing;

public class TemplateCommandParser : ICommandParser
{
    public object ParseCommandToDTO(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be null or empty.");
        
        string prefix = command.Split(' ')[0].ToUpper();

        if (!ProtocolRegistry.Templates.TryGetValue(prefix, out object template))
        {
            throw new NotSupportedException($"Command prefix '{prefix}' is not registered in the protocol.");
        }

        try
        {
            var parseMethod = template.GetType().GetMethod("Parse");
            
            if (parseMethod == null)
                throw new InvalidOperationException("Stored template object is missing a Parse method.");

            var result = parseMethod.Invoke(template, new object[] { command });

            if (result == null)
                throw new FormatException($"Failed to parse command: {command}");

            return result;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }
}