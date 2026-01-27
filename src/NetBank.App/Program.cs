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

public class Program
{
    static async Task Main(string[] args)
    {
        await RunServerAsync(args);
    }

    public static async Task RunServerAsync(string[] args, CancellationToken cancellationToken = default)
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
        
        var proxy = new SwappableStorageProxy(storage);
        var service = new AccountService(proxy, configuration, loggerFactory);

        var commandParser = new TemplateCommandParser();
        var commandExecutor = new CommandExecutor(service, commandParser, configuration);

        var server = new TcpCommandServer(
            commandExecutor, 
            configuration.ServerPort,
            IPAddress.Parse(configuration.ServerIp),
            configuration.InactivityTimeout,
            loggerFactory.CreateLogger<TcpCommandServer>()
        );

        var httpServer = new HttpServerHost(
            args,
            configuration,
            loggerFactory, 
            [service,proxy]
            );


        await Task.WhenAll(
            server.StartAsync(cancellationToken),
            httpServer.StartAsync()
        );
    }
}