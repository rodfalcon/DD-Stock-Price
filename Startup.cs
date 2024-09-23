using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using StockPriceApi.Models;
using StatsdClient;
using StockPriceApi.Data;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

public void ConfigureServices(IServiceCollection services)
{
        services.AddControllers();
        services.AddDbContext<StockPriceContext>(options =>
            options.UseSqlite(Configuration.GetConnectionString("StockPriceDatabase")));

        services.AddHttpClient();
        services.AddSingleton<IDogStatsd, DogStatsdService>(sp =>
        {
            var config = new StatsdConfig
            {
                StatsdServerName = "datadog-agent",
                StatsdPort = 8125,
            };

            var dogStatsdService = new DogStatsdService(config);

            return dogStatsdService;
        });

        // Register the StockPriceFetcherService as a hosted service
        services.AddHostedService<StockPriceFetcherService>();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowReactApp",
                builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
        });
        // Other service registrations...

        services.AddSpaStaticFiles(configuration =>
        {
            configuration.RootPath = "stock-price-frontend/build"; // Adjust the path according to your frontend project structure
        });
        // Additional configurations...
}

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseSpaStaticFiles();

        app.UseRouting();

        app.UseCors("AllowReactApp");

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        app.UseSpa(spa =>
        {
            spa.Options.SourcePath = "ClientApp";

            if (env.IsDevelopment())
            {
                spa.UseReactDevelopmentServer(npmScript: "start");
            }
        });
    }
}
