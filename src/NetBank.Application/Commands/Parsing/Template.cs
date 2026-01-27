using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NetBank.Commands.Parsing;

public class Template<T> where T : new()
{
    private readonly List<string> _literals = new();
    private readonly List<string> _placeholderNames = new();
    private readonly Dictionary<string, Action<T, string>> _setters = new(StringComparer.OrdinalIgnoreCase);
    
    // New: Compiled getters for string construction
    private readonly Dictionary<string, Func<T, string?>> _getters = new(StringComparer.OrdinalIgnoreCase);

    public Template(string template)
    {
        CompileTemplate(template);
        CompileSettersAndGetters();
    }

    private void CompileTemplate(string template)
    {
        var parts = template.Split('{', '}');
        for (int i = 0; i < parts.Length; i++)
        {
            // Even indexes are literals, Odd are placeholders
            if (i % 2 == 0) _literals.Add(parts[i]);
            else _placeholderNames.Add(parts[i]);
        }
    }

    private void CompileSettersAndGetters()
    {
        foreach (var propName in _placeholderNames)
        {
            var prop = typeof(T).GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null) continue;

            var instance = Expression.Parameter(typeof(T), "instance");

            // --- Compile Setters (Input -> DTO) ---
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
            var assign = Expression.Assign(Expression.Property(instance, prop), valueExpression);
            _setters[propName] = Expression.Lambda<Action<T, string>>(assign, instance, valueStr).Compile();

            // --- Compile Getters (DTO -> Output) ---
            var propertyAccess = Expression.Property(instance, prop);
            // Convert property value to string. Handles null by returning empty or calling ToString()
            var toStringCall = Expression.Call(
                Expression.Convert(propertyAccess, typeof(object)), 
                typeof(object).GetMethod("ToString")!
            );
            
            _getters[propName] = Expression.Lambda<Func<T, string?>>(toStringCall, instance).Compile();
        }
    }

    /// <summary>
    /// Constructs a string from a DTO based on the template.
    /// </summary>
    public string Construct(T dto)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < _literals.Count; i++)
        {
            sb.Append(_literals[i]);

            if (i < _placeholderNames.Count)
            {
                if (_getters.TryGetValue(_placeholderNames[i], out var getter))
                {
                    sb.Append(getter(dto));
                }
            }
        }

        return sb.ToString();
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

            if (nextLiteralPos < 0) throw new FormatException($"Input does not match template at literal segment: '{_literals[i+1]}'");

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