using System.Net;
using System.Net.Sockets;
using NetBank.App;

namespace EndToEndTests;

public class BankServerFixture : IAsyncLifetime
{
    public string Address => "127.0.0.1";
    public int Port { get; private set; }
    private CancellationTokenSource _cts = new();
    private Task? _serverTask;

    public async Task InitializeAsync()
    {
        Port = 5000;
        
        // Start the REAL app Program with the dynamic port
        //_serverTask = Program.RunServerAsync(
        //    new[] { "--ip", Address, "--port", Port.ToString() }, 
        //    _cts.Token);

        await Task.Delay(500); 
    }

    public async Task DisposeAsync()
    {
        await _cts.CancelAsync();
        if (_serverTask != null)
        {
            await Task.WhenAny(_serverTask, Task.Delay(2000));
        }
        _cts.Dispose();
    }
}