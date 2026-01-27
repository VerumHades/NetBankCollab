using System.Net;
using Microsoft.Extensions.Logging;
using NetBank.Commands;
using NetBank.Commands.Parsing;
using NetBank.Common;
using NetBank.Controllers.HttpController;
using NetBank.Controllers.TcpController;
using NetBank.Infrastructure;
using NetBank.Services;
using NetBank.Services.Implementations.DoubleBufferedAccountService;

namespace NetBank.App;

public class Program
{
    static async Task Main(string[] args)
    {
        SQLitePCL.Batteries.Init();

        await RunServerAsync(args);
    }

    public static async Task RunServerAsync(string[] args, CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= CancellationToken.None;
        
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
        var commandDelegator = new TcpCommandDelegator(IPAddress.Parse(configuration.ServerIp), configuration.DelegationTargetPort, loggerFactory.CreateLogger<TcpCommandDelegator>());
        var commandExecutor = new CommandExecutor(service, commandParser, commandDelegator, configuration);

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
            server.StartAsync(cancellationToken ?? CancellationToken.None),
            httpServer.StartAsync()
        );
    }
}