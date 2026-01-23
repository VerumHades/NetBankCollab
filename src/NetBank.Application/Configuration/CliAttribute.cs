namespace NetBank.Configuration
{
    /// <summary>
    /// Specifies the type of validation for a CLI option.
    /// </summary>
    public enum ValidationType
    {
        None,           // No validation
        MustBePositive, // Integer or long must be > 0
        NonEmptyString  // String must not be null or empty
        // Add more validation types as needed
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

        /// <summary>
        /// Validates a parsed value against the ValidationType specified in the attribute.
        /// Throws ArgumentException if validation fails.
        /// </summary>
        public void Validate(object? value)
        {
            switch (Validation)
            {
                case ValidationType.MustBePositive:
                    if(value == null)
                        throw new ArgumentException($"Option '{Name}' is required to be a non-null int or long.");
                    if (value is int intValue && intValue <= 0)
                        throw new ArgumentException($"Option '{Name}' must be positive.");
                    if (value is long longValue && longValue <= 0)
                        throw new ArgumentException($"Option '{Name}' must be positive.");
                    break;

                case ValidationType.NonEmptyString:
                    if (value == null || (value is string strValue && string.IsNullOrWhiteSpace(strValue)))
                        throw new ArgumentException($"Option '{Name}' is required to be a non-null string.");
                    break;

                case ValidationType.None:
                default:
                    break;
            }
        }
    }
}
