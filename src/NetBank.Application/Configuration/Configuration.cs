using System.Text.Json.Serialization;

namespace NetBank.Configuration;

public class Configuration
{
    [JsonPropertyName("serverIp")]
    [CliOption("--ip", "-i", "IP address for the orchestrator server", ValidationType.NonEmptyString)]
    public string ServerIp { get; set; } = "0.0.0.0";

    [JsonPropertyName("serverPort")]
    [CliOption("--port", "-p", "Port for the TCP orchestrator server", ValidationType.MustBePositive)]
    public int ServerPort { get; set; } = 5000;

    [JsonPropertyName("networkInactivityTimeoutMs")]
    [CliOption("--timeout", "-t", "Network inactivity timeout in milliseconds", ValidationType.MustBePositive)]
    public int NetworkInactivityTimeoutMs { get; set; } = 5000;

    [JsonPropertyName("bufferSwapDelayMs")]
    [CliOption("--swap-delay", "-d", "Delay before performing a buffer swap in milliseconds", ValidationType.MustBePositive)]
    public int BufferSwapDelayMs { get; set; } = 100;
    
    [JsonPropertyName("frontEndURl")]
    public string FrontEndURl { get; set; } = "https://localhost:8444";
    
    [JsonIgnore]
    public TimeSpan InactivityTimeout => TimeSpan.FromMilliseconds(NetworkInactivityTimeoutMs);

    [JsonIgnore]
    public TimeSpan BufferSwapDelay => TimeSpan.FromMilliseconds(BufferSwapDelayMs);
}