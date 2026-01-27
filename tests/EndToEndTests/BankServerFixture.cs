using System.Net;
using System.Net.Sockets;
using NetBank.App;

namespace EndToEndTests;

public class BankServerFixture : IAsyncLifetime
{
    public string Address => "127.0.0.1";
    public int Port { get; private set; }

    public string TargetAddress => "127.0.0.2";
    public int TargetPort { get; private set; }
    
    private CancellationTokenSource _cts = new();
    private Task? _serverTask;
    private Task? _serverTask2;

    public async Task InitializeAsync()
    {
        Port = 5000;
        TargetPort = 5009;
        
        //_serverTask = Program.RunServerAsync(
        //    new[] { "--ip", Address, "--port", Port.ToString(), "--sql-lite-filename", "testdb_1.db" }, 
        //    _cts.Token);
        
        //_serverTask2 = Program.RunServerAsync(
        //    new[] { "--ip", TargetAddress, "--port", $"{5000}", "--sql-lite-filename", "testdb_2.db" }, 
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
        if (_serverTask2 != null)
        {
            await Task.WhenAny(_serverTask2, Task.Delay(2000));
        }
        _cts.Dispose();
    }
}