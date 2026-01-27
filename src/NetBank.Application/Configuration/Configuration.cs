using System.Text.Json.Serialization;

namespace NetBank.Configuration;

public class Configuration
{
    [JsonPropertyName("serverIp")]
    [CliOption("--ip", "-i", "IP address for the orchestrator server", ValidationType.MustBeIpAddress)]
    public string ServerIp { get; set; } = "127.0.0.1";

    [JsonPropertyName("serverPort")]
    [CliOption("--port", "-p", "Port for the TCP orchestrator server", ValidationType.MustBePositive)]
    public int ServerPort { get; set; } = 5000;
    
    [JsonPropertyName("delegationTargetPort")]
    [CliOption("--delegation-target-port", description: "Target port for the TCP command delegator", validation: ValidationType.MustBePositive)]
    public int DelegationTargetPort { get; set; } = 5001;
    
    [JsonPropertyName("delegationTargetPort")]
    [CliOption("--delegation-target-port-range-start", description: "Target port for the TCP command delegator", validation:ValidationType.MustBePositive)]
    public int DelegationTargetPortRangeStart { get; set; } = 5000;
    
    [JsonPropertyName("delegationTargetPort")]
    [CliOption("--delegation-target-port-range-end", description: "Target port for the TCP command delegator", validation:ValidationType.MustBePositive)]
    public int DelegationTargetPortRangeEnd { get; set; } = 5010;

    [JsonPropertyName("networkInactivityTimeoutMs")]
    [CliOption("--timeout", "-t", "Network inactivity timeout in milliseconds", ValidationType.MustBePositive)]
    public int NetworkInactivityTimeoutMs { get; set; } = 5000;

    [JsonPropertyName("bufferSwapDelayMs")]
    [CliOption("--swap-delay", "-d", "Delay before performing a buffer swap in milliseconds", ValidationType.MustBePositive)]
    public int BufferSwapDelayMs { get; set; } = 100;
    
    [JsonPropertyName("sqliteFilename")]
    [CliOption("--sql-lite-filename", description: "Name of the sql lite database", validation: ValidationType.NonEmptyString)]
    public string SqlliteFilename { get; set; } = "database.db";
    
    [JsonPropertyName("frontEndURl")]
    public string FrontEndURl { get; set; } = "https://localhost:8444";
    
    [JsonIgnore]
    public TimeSpan InactivityTimeout => TimeSpan.FromMilliseconds(NetworkInactivityTimeoutMs);

    [JsonIgnore]
    public TimeSpan BufferSwapDelay => TimeSpan.FromMilliseconds(BufferSwapDelayMs);
}