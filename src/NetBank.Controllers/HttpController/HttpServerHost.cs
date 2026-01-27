using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NetBank.Services.Implementations.DoubleBufferedAccountService;

namespace NetBank.Controllers.HttpController;

/// <summary>
/// Hosts an HTTP server using ASP.NET Core, runs controllers derived from <see cref="HttpControllerBase"/>,
/// and provides Swagger documentation at /swagger.
/// <para>
/// Note: For local development with HTTPS, ensure that the ASP.NET Core development certificate is trusted
/// by running the following command once on your machine:
/// <c>dotnet dev-certs https --trust</c>
/// This will add the self-signed certificate to Windows trusted root store so clients like cURL or browsers
/// will trust the local HTTPS server.
/// </para>
/// </summary>
public class HttpServerHost
{
    private readonly WebApplication _app;
    private readonly ILogger<HttpServerHost> _logger;
    private readonly Configuration.Configuration _configuration;

    public HttpServerHost(
        Configuration.Configuration config,
        ILoggerFactory loggerFactory,
          Dictionary<Type, object> services)
    {
        _configuration = config; 
        _logger = loggerFactory.CreateLogger<HttpServerHost>();

        var builder = WebApplication.CreateBuilder();
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
        builder.Services.AddHttpClient();
        foreach (var kvp in services)
        {
            builder.Services.AddSingleton(kvp.Key, kvp.Value);
        }
        
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

    public Task StopAsync()
    {
        var url = _configuration.FrontEndURl;
        _logger.LogInformation("Stopping HTTPS server on {Url}", url);;
        return _app.StopAsync();
    }
}