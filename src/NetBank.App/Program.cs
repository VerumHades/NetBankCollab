using System.Net;
using Microsoft.Extensions.Logging;
using NetBank.Common;
using NetBank.Controllers.HttpController;
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

        var storage = new InMemoryStorageStrategy();
        
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
            configuration.BufferSwapDelay,
            loggerFactory.CreateLogger<ActivityDrivenTimer>()
        );
        
        var service = new AccountServiceBufferProxy(coordinator);
        service.OnActivity = () =>
        {
            swapTimer.WakeUp();
        };

        
        // need to run dotnet dev-certs https --trust
        var httpServer = new HttpServerHost(
            args,
            service,
            configuration,
            loggerFactory
        );

        _ = httpServer.StartAsync();
        
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