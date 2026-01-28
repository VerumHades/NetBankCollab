using Microsoft.Extensions.Logging.Abstractions;

namespace NetBank.Infrastructure;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

public sealed class TcpConnectionPool : IAsyncDisposable
{
    private readonly ILogger<TcpConnectionPool> _logger;
    private readonly int _portStart;
    private readonly int _portEnd;
    private readonly int _defaultPort;


    private readonly ConcurrentDictionary<IPAddress, ConcurrentBag<TcpClient>> _pool = new();
    private readonly ConcurrentDictionary<TcpClient, bool> _inUse = new();
    private readonly ConcurrentDictionary<IPAddress, int> _cachedPorts = new();

    public TcpConnectionPool(int portStart, int portEnd, int defaultPort, ILogger<TcpConnectionPool>? logger = null)
    {
        if (portEnd < portStart) throw new ArgumentException("portEnd must be >= portStart");

        _portStart = portStart;
        _portEnd = portEnd;
        _defaultPort = defaultPort;
        _logger = logger ?? NullLogger<TcpConnectionPool>.Instance;
    }

    /// <summary>
    /// Get a pooled or new TcpClient connected to the specified IP.
    /// </summary>
    public async Task<TcpClient> GetConnection(IPAddress ip)
    {
        var bag = _pool.GetOrAdd(ip, _ => new ConcurrentBag<TcpClient>());
        
        foreach (var client in bag)
        {
            if (client.Connected && _inUse.TryAdd(client, true))
            {
                _logger.LogDebug("Reusing existing connection to {Ip}", ip);
                return client;
            }
        }
        
        var newClient = await ConnectWithCachedPort(ip) ?? throw new InvalidOperationException($"Could not connect to any port on {ip}");

        bag.Add(newClient);
        _inUse.TryAdd(newClient, true);
        return newClient;
    }

    /// <summary>
    /// Marks a connection as no longer in use.
    /// </summary>
    public void ReleaseConnection(TcpClient client)
    {
        _inUse.TryRemove(client, out _);
    }

    private async Task<TcpClient?> ConnectWithCachedPort(IPAddress ip)
    {
        if (_cachedPorts.TryGetValue(ip, out int cachedPort))
        {
            var client = await TryConnect(ip, cachedPort);
            if (client != null) return client;
        }
        
        if (_defaultPort > 0)
        {
            var client = await TryConnect(ip, _defaultPort);
            if (client != null)
            {
                _cachedPorts[ip] = _defaultPort;
                return client;
            }
        }

        foreach (var port in Enumerable.Range(_portStart, _portEnd - _portStart + 1))
        {
            var client = await TryConnect(ip, port);
            if (client != null)
            {
                _cachedPorts[ip] = port;
                return client;
            }
        }

        return null;
    }

    private async Task<TcpClient?> TryConnect(IPAddress ip, int port)
    {
        try
        {
            var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(ip, port, cts.Token);
            _logger.LogInformation("Connected to {Ip}:{Port}", ip, port);
            return client;
        }
        catch
        {
            _logger.LogDebug("Port {Port} on {Ip} unavailable", port, ip);
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var bag in _pool.Values)
        {
            foreach (var client in bag)
            {
                try { client.Close(); client.Dispose(); } catch { }
            }
        }

        _pool.Clear();
        _inUse.Clear();
        _cachedPorts.Clear();
        await Task.CompletedTask;
    }
}
