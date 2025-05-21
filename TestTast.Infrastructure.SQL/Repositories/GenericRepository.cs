using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TestTask.Core.Interfaces;
using TestTask.Core.Repositories;
using TestTast.Infrastructure.SQL.Contexts;

namespace TestTast.Infrastructure.SQL.Repositories;

internal class GenericRepository<TEntity, TId> : IGenericRepository<TEntity, TId>
    where TEntity : class, IIdentifiablyEntity<TId>
{
    protected readonly ApplicationDbContext _context;

    public GenericRepository(ApplicationDbContext context) => _context = context;

    public void Create(TEntity entity) => _context.Set<TEntity>().Add(entity);

    public void CreateRange(IEnumerable<TEntity> entities) => _context.Set<TEntity>().AddRange(entities);

    public void Update(TEntity entity, params Expression<Func<TEntity, object?>>[] updatedProperties)
    {
        var dbEntityEntry = _context.Entry(entity);
        if (updatedProperties.Any())
        {
            foreach (var property in updatedProperties)
            {
                dbEntityEntry.Property(property).IsModified = true;
            }
        }
        else
        {
            _context.Set<TEntity>().Update(entity);
        }
    }

    public void Delete(TEntity entity) => _context.Set<TEntity>().Remove(entity);

    public async Task DeleteAsync(TId id)
    {
        var entity = await GetByIdAsync(id);

        if (entity != null) Delete(entity);
    }

    public async Task<TEntity?> GetByIdAsync(
        TId id,
        string? includeMany = null,
        Expression<Func<TEntity, object>>? include = null,
        bool withoutTracking = true,
        bool ignoreGlobalFilters = false,
        bool useSplitQuery = false)
    {
        return await GetQueryable(x => x.Id!.Equals(id), null, includeMany, include, null, null, withoutTracking, ignoreGlobalFilters)
            .FirstOrDefaultAsync();
    }

    public async Task<TEntity?> GetSingleAsync(
    Expression<Func<TEntity, bool>> filter,
    string? includeMany = null,
    Expression<Func<TEntity, object>>? include = null,
    bool withoutTracking = true,
    bool ignoreGlobalFilters = false,
    bool useSplitQuery = false)
    {
        return await GetQueryable(filter, null, includeMany, include, null, null, withoutTracking, ignoreGlobalFilters)
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string? includeMany = null,
        Expression<Func<TEntity, object>>? include = null,
        int? skip = null,
        int? take = null,
        bool withoutTracking = true,
        bool ignoreGlobalFilters = false,
        bool useSplitQuery = false)
    {
        return await GetQueryable(filter, orderBy, includeMany, include, skip, take, withoutTracking, ignoreGlobalFilters)
            .ToListAsync();
    }

    protected virtual IQueryable<TEntity> GetQueryable(
       Expression<Func<TEntity, bool>>? filter = null,
       Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
       string? includeMany = null,
       Expression<Func<TEntity, object>>? include = null,
       int? skip = null,
       int? take = null,
       bool withoutTracking = true,
       bool ignoreGlobalFilters = false,
       bool useSplitQuery = false)
    {
        IQueryable<TEntity> query = _context.Set<TEntity>();

        query = withoutTracking ? query.AsNoTrackingWithIdentityResolution() : query.AsTracking();

        if (filter != null) query = query.Where(filter);

        if (include != null) query = query.Include(include);

        if (!string.IsNullOrEmpty(includeMany))
        {
            query = includeMany
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Aggregate(query,
                    (current, property) => current.Include(property.Trim()));
        }

        if(useSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        if (orderBy != null) query = orderBy(query);

        if (skip != null) query = query.Skip(skip.Value);

        if (take != null) query = query.Take(take.Value);

        if (ignoreGlobalFilters) query = query.IgnoreQueryFilters();

        return query;
    }
}
