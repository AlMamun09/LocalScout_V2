using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LocalScout.Infrastructure.Data
{
    /// <summary>
    /// This factory is used by the Entity Framework Core design-time tools (e.g., for Add-Migration)
    /// to create an instance of the ApplicationDbContext.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Get the path to the appsettings.json in the LocalScout.Web project
            // This assumes the Web project is one level up and in a sibling folder
            string webProjectPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "../LocalScout.Web"
            );

            // Build configuration
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(webProjectPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Get the connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Could not find 'DefaultConnection' in appsettings.json."
                );
            }

            // Create the options builder
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(
                connectionString,
                b =>
                {
                    // We must tell it where the Migrations assembly is,
                    // just like we did in Program.cs
                    b.MigrationsAssembly("LocalScout.Infrastructure");
                }
            );

            // Return the new DbContext instance
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
