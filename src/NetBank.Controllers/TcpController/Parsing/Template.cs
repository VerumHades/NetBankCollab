using System.Linq.Expressions;
using System.Reflection;

namespace NetBank.Controllers.TcpController.Parsing;

public class Template<T> where T : new()
{
    private readonly List<string> _literals = new();
    private readonly List<string> _placeholderNames = new();
    private readonly Dictionary<string, Action<T, string>> _setters = new(StringComparer.OrdinalIgnoreCase);

    public Template(string template)
    {
        CompileTemplate(template);
        CompileSetters();
    }

    private void CompileTemplate(string template)
    {
        var parts = template.Split('{', '}');
        for (int i = 0; i < parts.Length; i++)
        {
            if (i % 2 == 0) _literals.Add(parts[i]);
            else _placeholderNames.Add(parts[i]);
        }
    }

    private void CompileSetters()
    {
        foreach (var propName in _placeholderNames)
        {
            var prop = typeof(T).GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null) continue;


            var instance = Expression.Parameter(typeof(T), "instance");
            var valueStr = Expression.Parameter(typeof(string), "value");


            Expression valueExpression;
            if (prop.PropertyType == typeof(string))
            {
                valueExpression = valueStr;
            }
            else
            {
                var parseMethod = prop.PropertyType.GetMethod("Parse", new[] { typeof(string) });
                
                if (parseMethod != null)
                {
                    valueExpression = Expression.Call(parseMethod, valueStr);
                }
                else
                {
                    var changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) });
                    valueExpression = Expression.Convert(
                        Expression.Call(changeTypeMethod, valueStr, Expression.Constant(prop.PropertyType)),
                        prop.PropertyType
                    );
                }
            }

            var propertyAccess = Expression.Property(instance, prop);
            var assign = Expression.Assign(propertyAccess, valueExpression);

            var lambda = Expression.Lambda<Action<T, string>>(assign, instance, valueStr);
            _setters[propName] = lambda.Compile();
        }
    }

    public T Parse(string input)
    {
        var result = new T();
        var span = input.AsSpan();
        int currentPos = 0;

        for (int i = 0; i < _placeholderNames.Count; i++)
        {
            currentPos += _literals[i].Length;

            int nextLiteralPos = (i + 1 < _literals.Count && !string.IsNullOrEmpty(_literals[i + 1])) 
                ? span.Slice(currentPos).IndexOf(_literals[i + 1]) 
                : span.Length - currentPos;

            if (nextLiteralPos < 0) throw new FormatException($"Input does not match template at '{_literals[i+1]}'");

            var valueStr = span.Slice(currentPos, nextLiteralPos).ToString();
            
            if (_setters.TryGetValue(_placeholderNames[i], out var setter))
            {
                setter(result, valueStr);
            }

            currentPos += nextLiteralPos;
        }

        return result;
    }
}