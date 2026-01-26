using System.Net;
using Microsoft.Extensions.Logging;
using NetBank.Common;
using NetBank.Common.Structures.Buffering;
using NetBank.Controllers.TcpController;
using NetBank.Controllers.TcpController.Parsing;
using NetBank.Infrastructure;
using NetBank.Services;
using NetBank.Services.Implementations.DoubleBufferedAccountService;

namespace NetBank.App;

class Program
{
    private class BufferFactory: IFactory<AccountServiceCaptureBuffer>
    {
        public AccountServiceCaptureBuffer Create()
        {
            return new AccountServiceCaptureBuffer();
        }
    }
    
    static async Task Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
        });
    
        Configuration.Configuration? configuration = null;
        try
        {
            configuration = new ConfigLoader<Configuration.Configuration>().Load(args);
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger<Program>().LogCritical(ex, "Failed to load configuration.");
            return;
        }

        var bufferFactory = new BufferFactory();
        var inmemStorage = new InMemoryStorageStrategy();
        var processor = new StorageBufferProcessor(inmemStorage);
        var buffer = new FlushOnSwapDoubleBuffer<AccountServiceCaptureBuffer>(bufferFactory, processor);
        
        using var observer = new TimedBufferObserver<AccountServiceCaptureBuffer>(
            () => {
                try { return buffer.TrySwap(); }
                catch (Exception e) { 
                    loggerFactory.CreateLogger("BufferObserver").LogError(e, "Buffer swap failed.");
                    return Task.FromResult(false); 
                }
            }, 
            buffer, 
            configuration.BufferSwapDelay
        );

        var serviceProvider = new LambdaProvider<IAccountService>(() => buffer.Front);
        var commandParser = new TemplateCommandParser();
        var commandExecutor = new CommandExecutor(serviceProvider, commandParser, configuration);

        var server = new TcpCommandServer(
            commandExecutor, 
            configuration.ServerPort,
            IPAddress.Parse(configuration.ServerIp),
            configuration.InactivityTimeout,
            loggerFactory.CreateLogger<TcpCommandServer>()
        );

        await server.StartAsync(CancellationToken.None);
    }
}