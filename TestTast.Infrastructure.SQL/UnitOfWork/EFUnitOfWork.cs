using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TestTask.Core.UnitOfWork;
using TestTast.Infrastructure.SQL.Contexts;

namespace TestTast.Infrastructure.SQL.UnitOfWork;

internal class EFUnitOfWork : IUnitOfWork
{
    protected readonly DbContext Context;
    private readonly ILogger<EFUnitOfWork> _logger;

    protected EFUnitOfWork(DbContext context, ILogger<EFUnitOfWork> logger)
    {
        Context = context;
        _logger = logger;
    }

    public async Task CommitChangesAsync()
    {
        try
        {
            await Context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                _logger.LogError($"Concurrency error in entity: {entry.Entity.GetType().Name}, ID: {entry.Property("Id")?.CurrentValue}");
            }
            throw;
        }
        //всякие другие ошибки...
    }

    public void CommitChanges() => Context.SaveChanges();
}
