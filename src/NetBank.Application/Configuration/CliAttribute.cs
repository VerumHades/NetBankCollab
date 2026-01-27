using System.Net;

namespace NetBank.Configuration
{
    public enum ValidationType
    {
        None,
        MustBePositive,
        NonEmptyString,
        MustBeIpAddress // New validation type
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CliOptionAttribute : Attribute
    {
        public string Name { get; }
        public string? ShortName { get; }
        public string? Description { get; }
        public ValidationType Validation { get; }

        public CliOptionAttribute(
            string name,
            string? shortName = null,
            string? description = null,
            ValidationType validation = ValidationType.None)
        {
            Name = name;
            ShortName = shortName;
            Description = description;
            Validation = validation;
        }

        public void Validate(object? value)
        {
            switch (Validation)
            {
                case ValidationType.MustBePositive:
                    if (value == null)
                        throw new ArgumentException($"Option '{Name}' is required.");
                    if (value is int intValue && intValue <= 0)
                        throw new ArgumentException($"Option '{Name}' must be positive.");
                    if (value is long longValue && longValue <= 0)
                        throw new ArgumentException($"Option '{Name}' must be positive.");
                    break;

                case ValidationType.NonEmptyString:
                    if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                        throw new ArgumentException($"Option '{Name}' cannot be empty.");
                    break;

                case ValidationType.MustBeIpAddress:
                    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        throw new ArgumentException($"Option '{Name}' is required.");

                    string ipString = value.ToString()!;
                    if (!IPAddress.TryParse(ipString, out _))
                    {
                        throw new ArgumentException($"Option '{Name}' contains an invalid IP address format: '{ipString}'.");
                    }
                    break;

                case ValidationType.None:
                default:
                    break;
            }
        }
    }
}