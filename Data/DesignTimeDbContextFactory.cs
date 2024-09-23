using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using StockPriceApi.Data;
using System.IO;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StockPriceContext>
{
    public StockPriceContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<StockPriceContext>();
        optionsBuilder.UseSqlite(configuration.GetConnectionString("StockPriceDatabase"));

        return new StockPriceContext(optionsBuilder.Options);
    }
}
