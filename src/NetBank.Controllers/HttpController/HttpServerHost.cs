using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NetBank.Services.Implementations.DoubleBufferedAccountService;

namespace NetBank.Controllers.HttpController;


public class HttpServerHost
{
    private readonly WebApplication _app;
    private readonly ILogger<HttpServerHost> _logger;
    private readonly Configuration.Configuration _configuration;

    public HttpServerHost(
        string[] args,
        AccountServiceBufferProxy accountService,
        Configuration.Configuration config,
        ILoggerFactory loggerFactory)
    {
        _configuration = config; 
        _logger = loggerFactory.CreateLogger<HttpServerHost>();

        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(HttpControllerBase).Assembly)
            .ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Clear();
                manager.FeatureProviders.Add(new DerivedControllerFeatureProvider(typeof(HttpControllerBase)));
            });
        
        builder.Services.AddSingleton(accountService);
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        _app = builder.Build();

        _app.UseRouting();

        _app.UseSwagger();
        _app.UseSwaggerUI();

        _app.MapControllers();
    }

    public Task StartAsync()
    {
        var url = _configuration.FrontEndURl;
        _logger.LogInformation("Starting HTTPS server on {Url}", url);
        return _app.RunAsync(url);
    }
}