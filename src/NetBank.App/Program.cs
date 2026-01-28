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
        try
        {
            SQLitePCL.Batteries.Init();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize sql lite libraries: {ex}");
        }
        
        try
        {
            await RunServerAsync(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected exception: {ex}");
        }
    }
    
    private static string GetValidLogPath(string preferredPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(preferredPath));
            if (string.IsNullOrEmpty(directory)) throw new Exception("Invalid log path");
            
            Directory.CreateDirectory(directory);

            string testFile = Path.Combine(directory, $".write-test-{Guid.NewGuid()}");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);

            return preferredPath;
        }
        catch
        {
            return Path.Combine(Path.GetTempPath(), "NetBank", "log-.txt");
        }
    }
    
    public static async Task RunServerAsync(string[] args, CancellationToken? cancellationToken = null)
    {
        Configuration.Configuration? configuration = null;
        try
        {
            configuration = new ConfigLoader<Configuration.Configuration>().Load(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load configuration: {ex}" );
            return;
        }
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(GetValidLogPath(configuration.LogFilename), 
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024,  
                rollOnFileSizeLimit: true,           
                retainedFileCountLimit: 5)              
            .CreateLogger();
        
        using var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddConsole().AddSerilog().SetMinimumLevel(LogLevel.Debug);
        });
        
        cancellationToken ??= CancellationToken.None;
        var programLogger = loggerFactory.CreateLogger<Program>();
        
        if (configuration.DelegationTargetPortRangeStart >= configuration.DelegationTargetPortRangeEnd)
        {
            programLogger.LogCritical("Delegation port range conform to: start < end.");
            return;
        }

        IStorageStrategy? storage = null;
        try
        {
            storage = new SqliteStorageStrategy(configuration.SqlliteFilename);
        }
        catch (Exception ex)
        {
            programLogger.LogError(ex, "Failed to load sql lite storage.");
            programLogger.LogWarning("Falling back to in memory storage strategy.");

            storage = new InMemoryStorageStrategy();
        }
        
        var proxy = new SwappableStorageProxy(storage);
        var service = new AccountService(proxy, configuration, loggerFactory);

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