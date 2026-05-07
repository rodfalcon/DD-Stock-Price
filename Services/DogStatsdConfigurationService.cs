using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using StatsdClient;

/// <summary>
/// Configures Statsd client before other hosted jobs emit metrics (runs early by registration order).
/// </summary>
public sealed class DogStatsdConfigurationService : IHostedService
{
    private readonly ILogger<DogStatsdConfigurationService> _logger;
    private readonly IConfiguration _configuration;

    public DogStatsdConfigurationService(ILogger<DogStatsdConfigurationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var section = _configuration.GetSection("DogStatsd");

        // Allow turning off DogStatsd for local dotnet run without agent.
        if (!section.GetValue("Enabled", true))
        {
            _logger.LogInformation("DogStatsd disabled via configuration.");
            return Task.CompletedTask;
        }

        var host = FirstNonEmpty(
            Environment.GetEnvironmentVariable("DOGSTATSD_HOST"),
            section["Host"],
            "127.0.0.1");

        var port = FirstPort(
            Environment.GetEnvironmentVariable("DOGSTATSD_PORT"),
            Environment.GetEnvironmentVariable("DD_DOGSTATSD_PORT"),
            section["Port"]);

        try
        {
            var config = new StatsdConfig
            {
                StatsdServerName = host,
                StatsdPort = port,
                Prefix = "",
                ConstantTags = new[]
                {
                    "service:stock-price-api",
                },
            };

            DogStatsd.Configure(config, ex => _logger.LogWarning(ex, "DogStatsd client error."));
            _logger.LogInformation("DogStatsd configured for {Host}:{Port}.", host, port);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DogStatsd could not be configured; custom metrics may be unavailable.");
        }

        return Task.CompletedTask;

        static string FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v!;
            }
            return "127.0.0.1";
        }

        static int FirstPort(params string?[] candidates)
        {
            foreach (var c in candidates)
            {
                if (int.TryParse(c, out var p) && p > 0)
                    return p;
            }
            return StatsdConfig.DefaultStatsdPort;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            DogStatsd.Dispose();
        }
        catch
        {
            // ignore shutdown faults
        }
        return Task.CompletedTask;
    }
}
