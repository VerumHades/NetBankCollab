namespace EndToEndTests;

public class BankServerFixture : IAsyncLifetime
{
    public string Address => "127.0.0.1";
    public int Port { get; private set; }
    private CancellationTokenSource _cts = new();
    private Task? _serverTask;

    public async Task InitializeAsync()
    {
        Port = 9005; // Or use a dynamic port helper
        
        // Start the REAL app Program
        _serverTask = NetBank.App.Program.RunServerAsync(
            new[] { "--ip", Address, "--port", $"{Port}" }, 
            _cts.Token);

        // Wait for the server to be ready
        await Task.Delay(500); 
    }

    public async Task DisposeAsync()
    {
        _cts.Cancel();
        if (_serverTask != null)
        {
            await Task.WhenAny(_serverTask, Task.Delay(1000));
        }
        _cts.Dispose();
    }
}