using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CanHappy.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = String.IsNullOrEmpty(configuration.GetConnectionString("DefaultConnection")) ?  
            
                String.IsNullOrEmpty(configuration.GetConnectionString("LocalConnection")) ? 
                throw new InvalidOperationException("Connection string 'LocalConnection' not found.") 
                : configuration.GetConnectionString("LocalConnection")
            
            : configuration.GetConnectionString("DefaultConnection")
            ;
        // var connectionString = configuration.GetConnectionString("DefaultConnection")
        //     ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");    

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
