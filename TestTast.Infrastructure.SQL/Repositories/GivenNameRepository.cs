using TestTask.Core.Entities;
using TestTask.Core.Repositories;
using TestTast.Infrastructure.SQL.Contexts;

namespace TestTast.Infrastructure.SQL.Repositories;

internal class GivenNameRepository : GenericRepository<GivenName, Guid>, IGivenNameRepository
{
    public GivenNameRepository(ApplicationDbContext context) : base(context)
    {
    }
}
