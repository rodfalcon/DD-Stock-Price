using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.Datadog.Logs;
using StockPriceApi.Data; // Adjust the namespace to match your context

namespace StockPriceApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Instantiate the logger
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(new JsonFormatter(renderMessage: true), "/app/logs/log.json", rollingInterval: RollingInterval.Day)
            .CreateLogger();

            try
            {
                Log.Information("Starting the web host");
                var host = CreateHostBuilder(args).Build();
                ApplyMigrations(host);
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "The application failed to start correctly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush(); // Ensure all logs are flushed on shutdown
            }

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://*:80");
                });

        private static void ApplyMigrations(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<StockPriceContext>();
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while applying migrations.");
                throw;
            }
        }
    }
}
