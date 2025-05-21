using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TestTask.Core.Initializers;
using TestTast.Infrastructure.SQL.Contexts;

namespace TestTast.Infrastructure.SQL.Initializers;

internal class DatabaseInitializer(
    ApplicationDbContext dbContext, 
    ILogger<DatabaseInitializer> logger) : IApplicationInitializer
{
    public async Task InitializeAsync()
    {
        try
        {
            logger.LogInformation("Applying database migrations...");

            await dbContext.Database.MigrateAsync();

            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while applying database migrations.");
            throw;
        }
    }
}
