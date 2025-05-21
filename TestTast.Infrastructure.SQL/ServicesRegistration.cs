using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TestTask.Core.Initializers;
using TestTask.Core.Repositories;
using TestTask.Core.UnitOfWork;
using TestTast.Infrastructure.SQL.Contexts;
using TestTast.Infrastructure.SQL.Initializers;
using TestTast.Infrastructure.SQL.Repositories;
using TestTast.Infrastructure.SQL.UnitOfWork;

namespace TestTast.Infrastructure.SQL;

public static class ServicesRegistration
{
    public static void AddInfrastructureSQL(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
           options.UseSqlServer(
               configuration.GetConnectionString("ApplicationDatabaseConnection"),
               b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IHumanNameRepository, HumanNameRepository>();
        services.AddScoped<IGivenNameRepository, GivenNameRepository>();

        services.AddScoped<IApplicationDatabase, ApplicationDatabase>();
        services.AddScoped<IApplicationInitializer, DatabaseInitializer>();
    }
}
