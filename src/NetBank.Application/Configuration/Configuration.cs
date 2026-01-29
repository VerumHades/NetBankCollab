using System.Text.Json.Serialization;

namespace NetBank.Configuration;

public class Configuration
{
    [JsonPropertyName("serverIp")]
    [CliOption("--ip", "-i", "IP address for the orchestrator server", ValidationType.MustBeIpAddress)]
    public string ServerIp { get; set; } = "10.2.7.141";

    [JsonPropertyName("serverPort")]
    [CliOption("--port", "-p", "Port for the TCP orchestrator server", ValidationType.MustBePositive)]
    public int ServerPort { get; set; } = 65525;
    
    [JsonPropertyName("delegationTargetPort")]
    [CliOption("--delegation-target-port", description: "Target port for the TCP command delegator", validation: ValidationType.MustBePositive)]
    public int DelegationTargetPort { get; set; } = 65525;
    
    [JsonPropertyName("delegationTargetPort")]
    [CliOption("--delegation-target-port-range-start", description: "Target port for the TCP command delegator", validation:ValidationType.MustBePositive)]
    public int DelegationTargetPortRangeStart { get; set; } = 65525;
    
    [JsonPropertyName("delegationTargetPort")]
    [CliOption("--delegation-target-port-range-end", description: "Target port for the TCP command delegator", validation:ValidationType.MustBePositive)]
    public int DelegationTargetPortRangeEnd { get; set; } = 65535;

    [JsonPropertyName("networkInactivityTimeoutMs")]
    [CliOption("--timeout", "-t", "Network inactivity timeout in milliseconds", ValidationType.MustBePositive)]
    public int NetworkInactivityTimeoutMs { get; set; } = 10000;

    [JsonPropertyName("bufferSwapDelayMs")]
    [CliOption("--swap-delay", "-d", "Delay before performing a buffer swap in milliseconds", ValidationType.MustBePositive)]
    public int BufferSwapDelayMs { get; set; } = 50;
    
    [JsonPropertyName("sqliteFilename")]
    [CliOption("--sql-lite-filename", description: "Filename of the sql lite database", validation: ValidationType.NonEmptyString)]
    public string SqlliteFilename { get; set; } = "database.db";
    
    [JsonPropertyName("logFilepath")]
    [CliOption("--log-filepath", description: "Filepath of the log.", validation: ValidationType.MustBeValidPath)]
    public string LogFilename { get; set; } = "log.txt";
    
    [JsonPropertyName("frontEndURl")]
    public string FrontEndURl { get; set; } = "http://localhost:8444";
    
    [JsonIgnore]
    public TimeSpan InactivityTimeout => TimeSpan.FromMilliseconds(NetworkInactivityTimeoutMs);

    [JsonIgnore]
    public TimeSpan BufferSwapDelay => TimeSpan.FromMilliseconds(BufferSwapDelayMs);
}