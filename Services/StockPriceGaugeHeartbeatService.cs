using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockPriceApi.Data;

/// <summary>
/// Re-sends last stored USD price as <c>stock_price.latest</c> gauge on an interval — no Alpha Vantage calls.
/// Keeps Datadog dashboards filled between scheduled API fetches.
/// </summary>
public sealed class StockPriceGaugeHeartbeatService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IStockQuoteTelemetry _quoteTelemetry;
    private readonly ILogger<StockPriceGaugeHeartbeatService> _logger;

    private static readonly string[] Symbols = { "DDOG", "DT", "NEWR" };

    public StockPriceGaugeHeartbeatService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IStockQuoteTelemetry quoteTelemetry,
        ILogger<StockPriceGaugeHeartbeatService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _quoteTelemetry = quoteTelemetry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue("StockPrice:GaugeHeartbeatMinutes", 15);
        if (intervalMinutes <= 0)
        {
            _logger.LogInformation("StockPrice:GaugeHeartbeatMinutes is {Minutes}; gauge heartbeat disabled.", intervalMinutes);
            return;
        }

        _logger.LogInformation(
            "Stock price gauge heartbeat every {Minutes} minutes (SQLite only; channel=database_heartbeat).",
            intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<StockPriceContext>();

                foreach (var symbol in Symbols)
                {
                    var latest = await context.StockPrices
                        .AsNoTracking()
                        .Where(sp => sp.Symbol == symbol)
                        .OrderByDescending(sp => sp.Timestamp)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (latest != null)
                    {
                        _quoteTelemetry.RecordLatestUsdGauge(symbol, latest.Price, "database_heartbeat");
                        _quoteTelemetry.RecordChangePercentGauge(symbol, latest.ChangePercent, "database_heartbeat");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gauge heartbeat tick failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
