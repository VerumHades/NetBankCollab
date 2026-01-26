using NetBank.Configuration;
using NetBank.Infrastructure;

namespace TestProject1;

public class MockConfiguration
{
    [CliOption("--ip", "-i", "IP address", ValidationType.NonEmptyString)]
    public string ServerIp { get; set; } = "0.0.0.0";

    [CliOption("--port", "-p", "Port", ValidationType.MustBePositive)]
    public int ServerPort { get; set; } = 5000;

    [CliOption("--timeout", "-t", "Timeout", ValidationType.MustBePositive)]
    public int NetworkInactivityTimeoutMs { get; set; } = 5000;
}

public class ConfigLoaderEdgeTests
{
    private readonly ConfigLoader<MockConfiguration> _loader = new();

    [Fact]
    public void Load_ValidFullArguments_UpdatesAllProperties()
    {
        string[] args = { "--ip", "192.168.1.1", "-p", "8080", "-t", "1000" };

        var config = _loader.Load(args);

        Assert.Equal("192.168.1.1", config.ServerIp);
        Assert.Equal(8080, config.ServerPort);
        Assert.Equal(1000, config.NetworkInactivityTimeoutMs);
    }

    [Fact]
    public void Load_MustBePositive_ThrowsWhenZero()
    {
        // Edge Case: ValidationType.MustBePositive
        string[] args = { "--port", "0" };

        // This assumes attribute.Validate(converted) throws an exception on failure
        Assert.ThrowsAny<Exception>(() => _loader.Load(args));
    }
    
    [Fact]
    public void Load_MustBePositive_ThrowsWhenNegative()
    {
        // Edge Case: ValidationType.MustBePositive
        string[] args = { "--port", "-100" };

        // This assumes attribute.Validate(converted) throws an exception on failure
        Assert.ThrowsAny<Exception>(() => _loader.Load(args));
    }

    [Fact]
    public void Load_NonEmptyString_ThrowsWhenValueIsMissing()
    {
        // Edge Case: Key provided but next arg is another key (leaving value null)
        string[] args = { "--ip", "--port", "5000" };

        var ex = Assert.Throws<ArgumentException>(() => _loader.Load(args));
        // Verify it failed specifically on the IP or the validation
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void Load_MalformedInteger_ThrowsConversionError()
    {
        // Edge Case: Type mismatch (String passed to Int property)
        string[] args = { "-p", "not-a-number" };

        var ex = Assert.Throws<ArgumentException>(() => _loader.Load(args));
        Assert.Contains("Expected Int32", ex.Message);
    }

    [Fact]
    public void Load_IncompletePairAtEnd_ThrowsException()
    {
        // Edge Case: The very last argument is a key with no value
        string[] args = { "--ip", "1.1.1.1", "--port" };

        Assert.ThrowsAny<Exception>(() => _loader.Load(args));
    }

    [Fact]
    public void Load_DuplicateArguments_LastOneWins()
    {
        // Edge Case: User provides the same flag twice
        string[] args = { "-p", "1000", "-p", "2000" };

        var config = _loader.Load(args);

        // Standard behavior for reflection-based setters: the last write wins.
        Assert.Equal(2000, config.ServerPort);
    }
}