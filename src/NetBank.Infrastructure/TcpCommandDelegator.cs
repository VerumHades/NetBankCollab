using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NetBank.Commands;

public class TcpCommandDelegator : ICommandDelegator
{
    private readonly int _targetPort;
    private readonly IPAddress _configuredIp;
    private readonly ILogger<TcpCommandDelegator> _logger;
    private readonly HashSet<IPAddress> _localInterfaces;

    public TcpCommandDelegator(
        IPAddress configuredIp, 
        int targetPort, 
        ILogger<TcpCommandDelegator>? logger = null)
    {
        _configuredIp = configuredIp;
        _targetPort = targetPort;
        _logger = logger ?? NullLogger<TcpCommandDelegator>.Instance;
        
        _localInterfaces = GetLocalIpAddresses();
    }

    public bool ShouldBeDelegated(string address)
    {
        if (!IPAddress.TryParse(address, out var targetIp))
        {
            _logger.LogWarning("Invalid IP address format: {Address}", address);
            return false;
        }
        
        if (_configuredIp.Equals(IPAddress.Any))
        {
            return !_localInterfaces.Contains(targetIp);
        }
        
        return !(_configuredIp.Equals(targetIp) || IPAddress.IsLoopback(targetIp));
    }

    public async Task<string> DelegateTextCommand(string commandString, string address)
    {
        _logger.LogInformation("Delegating to {Address}:{Port}", address, _targetPort);

        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await client.ConnectAsync(address, _targetPort, cts.Token);

            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            await writer.WriteLineAsync(commandString);
            return await reader.ReadLineAsync(cts.Token) ?? "Error: No response from peer.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delegation failed for {Address}", address);
            return $"Error: Peer communication failure ({address}).";
        }
    }

    private HashSet<IPAddress> GetLocalIpAddresses()
    {
        var ips = new HashSet<IPAddress> { IPAddress.Loopback, IPAddress.IPv6Loopback };
        try
        {
            var hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var ip in hostAddresses) ips.Add(ip);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to resolve network interfaces.");
        }
        return ips;
    }
}