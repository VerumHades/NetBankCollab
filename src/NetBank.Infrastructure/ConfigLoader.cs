using System.Reflection;
using System.Text.Json;
using NetBank.Configuration;

namespace NetBank.Infrastructure
{
    public class ConfigLoader<T> where T : new()
    {
        private Dictionary<string, (PropertyInfo, CliOptionAttribute)> options = new();
        public ConfigLoader()
        {
            BuildOptionMap();
        }

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
        /// Loads config from JSON (if --config is present) and applies CLI overrides.
        /// </summary>
        public T Load(string[] args)
        {
            var config = new T();

            string? configPath = ReadConfigPath(args);
            if (configPath != null)
                config = LoadFromJson(config, configPath);

            ApplyCliOverrides(config, args);

            return config;
        }

        private static string? ReadConfigPath(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "--config")
                    return args[i + 1];

            return null;
        }

        public T LoadFromJson(T existingConfig, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Config file not found: {path}");

            var text = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(text) ?? existingConfig;
        }

        private void ApplyCliOverrides(T config, string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var key = args[i];

                if (!key.StartsWith('-'))
                    throw new ArgumentException("Unexpected argument: " + key);

                if(key == "--config"){
                    i++;
                    continue;
                }
                
                if (!options.TryGetValue(key, out (PropertyInfo, CliOptionAttribute) option))
                    throw new ArgumentException("Unknown cli options: " + key);

                var (property, attribute) = option;

                string? value = null;

                if (i + 1 < args.Length && !args[i + 1].StartsWith('-')) {
                    value = args[i + 1];
                    i++;
                }
                

                object? converted = null;

                if (property.PropertyType == typeof(bool))
                {
                    converted = string.IsNullOrEmpty(value) ? true : bool.Parse(value);
                }
                else if(value != null)
                {
                    converted = Convert.ChangeType(value, property.PropertyType);
                }

                attribute.Validate(converted);
                property.SetValue(config, converted);
            }
        }
    }
}