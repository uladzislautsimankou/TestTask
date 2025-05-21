using TestTask.Core.Repositories;

namespace TestTask.Core.UnitOfWork;

public interface IApplicationDatabase : IUnitOfWork
{
    IPatientRepository Patients {  get; }

    IHumanNameRepository HumanNames { get; }

    IGivenNameRepository GivenNames { get; }
}
