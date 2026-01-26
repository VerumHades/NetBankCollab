using System.Reflection;
using System.Text.Json;
using NetBank.Configuration;

namespace NetBank.Infrastructure;

/// <summary>
/// A generic utility for hydrating configuration objects from JSON files and command-line arguments.
/// </summary>
/// <typeparam name="T">The configuration type to load, which must have a parameterless constructor.</typeparam>
public class ConfigLoader<T> where T : new()
{
    /// <summary>
    /// Internal mapping of CLI flags (e.g., "--port" or "-p") to their corresponding property and attribute metadata.
    /// </summary>
    private Dictionary<string, (PropertyInfo, CliOptionAttribute)> options = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigLoader{T}"/> class and builds the option map via reflection.
    /// </summary>
    public ConfigLoader()
    {
        BuildOptionMap();
    }

    /// <summary>
    /// Scans the properties of <typeparamref name="T"/> for <see cref="CliOptionAttribute"/> and populates the internal lookup dictionary.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if duplicate CLI names or short names are detected.</exception>
    private void BuildOptionMap()
    {
        foreach (var property in typeof(T).GetProperties())
        {
            var attribute = property.GetCustomAttribute<CliOptionAttribute>();
            if (attribute == null) continue;

            if (attribute.Name != null)
            {
                if (options.ContainsKey(attribute.Name))
                    throw new InvalidOperationException($"Duplicate CLI option: {attribute.Name}");
                options[attribute.Name] = (property, attribute);
            }

            if (attribute.ShortName != null)
            {
                if (options.ContainsKey(attribute.ShortName))
                    throw new InvalidOperationException($"Duplicate CLI short option: {attribute.ShortName}");
                options[attribute.ShortName] = (property, attribute);
            }
        }
    }

    /// <summary>
    /// Loads configuration data by first checking for a JSON file path in arguments, 
    /// then applying any command-line overrides.
    /// </summary>
    /// <param name="args">The command-line arguments array.</param>
    /// <returns>A fully populated instance of <typeparamref name="T"/>.</returns>
    public T Load(string[] args)
    {
        var config = new T();

        string? configPath = ReadConfigPath(args);
        if (configPath != null)
            config = LoadFromJson(config, configPath);

        ApplyCliOverrides(config, args);

        return config;
    }

    /// <summary>
    /// Specifically searches the arguments for the "--config" flag to locate the settings file.
    /// </summary>
    /// <param name="args">The command-line arguments array.</param>
    /// <returns>The path to the config file if found; otherwise, null.</returns>
    private static string? ReadConfigPath(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--config")
                return args[i + 1];

        return null;
    }

    /// <summary>
    /// Deserializes a JSON file into an existing configuration instance.
    /// </summary>
    /// <param name="existingConfig">The current configuration object to populate.</param>
    /// <param name="path">The file system path to the JSON file.</param>
    /// <returns>The updated configuration object.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
    public T LoadFromJson(T existingConfig, string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Config file not found: {path}");

        var text = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(text) ?? existingConfig;
    }

    /// <summary>
    /// Iterates through command-line arguments and maps them to the properties of the configuration object.
    /// </summary>
    /// <param name="config">The configuration instance to modify.</param>
    /// <param name="args">The command-line arguments array.</param>
    /// <exception cref="ArgumentException">Thrown if arguments are malformed or types are incompatible.</exception>
    private void ApplyCliOverrides(T config, string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var key = args[i];

            if (IsConfigFlag(key)) { i++; continue; }
            
            var (property, attribute) = GetOptionOrThrow(key);
            
            string? rawValue = ExtractValue(args, ref i);
            
            object? converted = ConvertValue(rawValue, property.PropertyType, key);
            attribute.Validate(converted);
            
            property.SetValue(config, converted);
        }
    }

    private bool IsConfigFlag(string key) => key == "--config";

    private (PropertyInfo, CliOptionAttribute) GetOptionOrThrow(string key)
    {
        if (!key.StartsWith('-')) throw new ArgumentException($"Unexpected argument: {key}");
        if (!options.TryGetValue(key, out var option)) throw new ArgumentException($"Unknown option: {key}");
        return option;
    }

    private string? ExtractValue(string[] args, ref int index)
    {
        if (index + 1 < args.Length && !args[index + 1].StartsWith('-'))
        {
            return args[++index];
        }
        return null;
    }

    private object? ConvertValue(string? value, Type targetType, string key)
    {
        if (targetType == typeof(bool))
            return string.IsNullOrEmpty(value) || bool.Parse(value);

        if (value == null) return null;

        try { return Convert.ChangeType(value, targetType); }
        catch { throw new ArgumentException($"Invalid value '{value}' for '{key}'. Expected {targetType.Name}."); }
    }
}
