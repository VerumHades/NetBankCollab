using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NetBank.Infrastructure;

namespace NetBank.Commands;

public class TcpCommandDelegator : ICommandDelegator, IAsyncDisposable
{
    private readonly IPAddress _configuredIp;
    private readonly ILogger<TcpCommandDelegator> _logger;
    private readonly HashSet<IPAddress> _localInterfaces;
    private readonly TcpConnectionPool _connectionPool;

    public TcpCommandDelegator(
        IPAddress configuredIp,
        int portStart,
        int portEnd,
        int defaultPort,
        ILogger<TcpCommandDelegator>? logger = null,
        ILogger<TcpConnectionPool>? poolLogger = null)
    {
        _configuredIp = configuredIp;
        _logger = logger ?? NullLogger<TcpCommandDelegator>.Instance;

        _localInterfaces = GetLocalIpAddresses();

        _connectionPool = new TcpConnectionPool(
            portStart, portEnd, defaultPort, poolLogger);
    }

    /// <summary>
    /// Determine if a command should be delegated to another IP.
    /// </summary>
    public bool ShouldBeDelegated(string address)
    {
        if (!IPAddress.TryParse(address, out var targetIp))
        {
            _logger.LogWarning("Invalid IP address format: {Address}", address);
            return false;
        }

        if (_configuredIp.Equals(IPAddress.Any))
            return !_localInterfaces.Contains(targetIp);

        return !_configuredIp.Equals(targetIp);
    }

    /// <summary>
    /// Delegate a text command using the pooled TCP connections.
    /// </summary>
    public async Task<string> DelegateTextCommand(string commandString, string address)
    {
        if (!IPAddress.TryParse(address, out var ip))
        {
            _logger.LogWarning("Invalid IP address format: {Address}", address);
            return $"ER Invalid IP ({address})";
        }

        TcpClient client = await _connectionPool.GetConnection(ip);

        try
        {
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

            await writer.WriteLineAsync(commandString);
            return await reader.ReadLineAsync() ?? "ER No response from peer.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delegation failed for {Address}", address);
            client.Close(); // Mark as unusable
            return $"ER Peer communication failure ({address}).";
        }
        finally
        {
            _connectionPool.ReleaseConnection(client);
        }
    }

    private HashSet<IPAddress> GetLocalIpAddresses()
    {
        var ips = new HashSet<IPAddress> { IPAddress.Loopback, IPAddress.IPv6Loopback };
        try
        {
            foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
                ips.Add(ip);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to resolve network interfaces.");
        }
        return ips;
    }

    /// <summary>
    /// Dispose the pool and all pooled connections.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _connectionPool.DisposeAsync();
    }
}
