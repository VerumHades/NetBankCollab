using System.Net;
using Microsoft.Extensions.Logging;
using NetBank.Commands;
using NetBank.Commands.Parsing;
using NetBank.Common;
using NetBank.Controllers.HttpController;
using NetBank.Controllers.TcpController;
using NetBank.Infrastructure;
using NetBank.Services;
using NetBank.Services.Implementations;
using NetBank.Services.Implementations.DoubleBufferedAccountService;
using NetBank.Services.NetworkScan;

namespace NetBank.App;

public class Program
{
    static async Task Main(string[] args)
    {
        SQLitePCL.Batteries.Init();
    
        using var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
        });

        try
        {
            await RunServerAsync(args, loggerFactory);
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger<Program>().LogCritical(ex, "Unexpected exception.");
        }
    }

    public static async Task RunServerAsync(string[] args, ILoggerFactory loggerFactory, CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= CancellationToken.None;
    
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

        var storage = new SqliteStorageStrategy(configuration.SqlliteFilename);
        
        var proxy = new SwappableStorageProxy(storage);
        var service = new AccountService(proxy, configuration, loggerFactory);

        if (configuration.DelegationTargetPortRangeStart >= configuration.DelegationTargetPortRangeEnd)
        {
            loggerFactory.CreateLogger<Program>().LogCritical("Delegation port range conform to: start < end.");
            return;
        }

        var commandParser = new TemplateCommandParser();
        var commandDelegator = new TcpCommandDelegator(
            IPAddress.Parse(configuration.ServerIp), 
            configuration.DelegationTargetPortRangeStart, 
            configuration.DelegationTargetPortRangeEnd, 
            configuration.DelegationTargetPort, 
            loggerFactory.CreateLogger<TcpCommandDelegator>());
        var commandExecutor = new CommandExecutor(service, commandParser, commandDelegator, configuration);

        var server = new TcpCommandServer(
            commandExecutor, 
            configuration.ServerPort,
            IPAddress.Parse(configuration.ServerIp),
            configuration.InactivityTimeout,
            loggerFactory.CreateLogger<TcpCommandServer>()
        );
        var networStore = new InMemoryScanProgressStrategy();
        var networkScanner = new NetworkScanService(networStore,loggerFactory.CreateLogger<NetworkScanService>());
        
        var servicesToRegister = new Dictionary<Type, object>
        {
            { typeof(IAccountService), service },
            { typeof(SwappableStorageProxy), proxy },
            { typeof(NetworkScanService), networkScanner }
        };

        
        var httpServer = new HttpServerHost(
            configuration,
            loggerFactory, 
            servicesToRegister
            );

        await Task.WhenAll(
            server.StartAsync(cancellationToken ?? CancellationToken.None),
            httpServer.StartAsync()
        );
    }
}