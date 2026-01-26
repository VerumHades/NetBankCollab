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

        var storage = new SqliteStorageStrategy();
        
        var processor = new CapturedAccountActionsProcessor(storage);
        var coordinator = new DoubleBufferedAccountCoordinator(processor);
        
        using var swapTimer = new ActivityDrivenTimer(
            () =>
            {
                try
                {
                    return coordinator.TrySwap();
                }
                catch (Exception e)
                {
                    loggerFactory.CreateLogger("BufferObserver").LogError(e, "Buffer swap failed.");
                    return Task.FromResult(false);
                }
            }, 
            configuration.BufferSwapDelay
        );
        
        var service = new AccountServiceBufferProxy(coordinator);
        service.OnActivity = () =>
        {
            swapTimer.WakeUp();
        };

        var commandParser = new TemplateCommandParser();
        var commandExecutor = new CommandExecutor(service, commandParser, configuration);

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