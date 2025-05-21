using TestTask.Core.Entities;
using TestTask.Core.Repositories;
using TestTast.Infrastructure.SQL.Contexts;

namespace TestTast.Infrastructure.SQL.Repositories;

internal class HumanNameRepository : GenericRepository<HumanName, Guid>, IHumanNameRepository
{
    public HumanNameRepository(ApplicationDbContext context) : base(context)
    {
    }
}
