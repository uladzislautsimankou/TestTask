using Microsoft.Extensions.Logging;
using TestTask.Core.Repositories;
using TestTask.Core.UnitOfWork;
using TestTast.Infrastructure.SQL.Contexts;

namespace TestTast.Infrastructure.SQL.UnitOfWork;

internal class ApplicationDatabase : EFUnitOfWork, IApplicationDatabase
{
    public ApplicationDatabase(
        ApplicationDbContext context,
        IPatientRepository patients,
        IHumanNameRepository humanNames,
        IGivenNameRepository givenNames,
        ILogger<EFUnitOfWork> logger
        ) : base(context, logger)
    {
        Patients = patients;
        HumanNames = humanNames;
        GivenNames = givenNames;
    }

    public IPatientRepository Patients { get; }

    public IHumanNameRepository HumanNames { get; }

    public IGivenNameRepository GivenNames { get; }
}
