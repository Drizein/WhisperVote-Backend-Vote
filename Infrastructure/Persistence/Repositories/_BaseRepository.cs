using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Application.Interfaces;

namespace Infrastructure.Persistence.Repositories;

public abstract class _BaseRepository<T> : _IBaseRepository<T> where T : class
{
    private readonly CDbContext _dbContext;
    protected readonly DbSet<T> DbSet;

    protected _BaseRepository(
        CDbContext dbContext
    )
    {
        _dbContext = dbContext;
        DbSet = _dbContext.Set<T>();
    }

    // read from database?
    public virtual async Task<IEnumerable<T>> SelectAsync()
    {
        return await DbSet.AsNoTracking().ToListAsync();
        // not loaded into repository, ie.not tracked
    }

    //public virtual async Task<T?> FindByIdAsync(Guid id) =>
    //await DbSet.FirstOrDefaultAsync(item => item.Guid == id);

    //                                                       LINQ expression trees   
    public virtual async Task<IEnumerable<T>> FilterAsync(Expression<Func<T, bool>> p)
    {
        return await DbSet.Where(p).ToListAsync();
    }

    public virtual async Task<T?> FindByAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.FirstOrDefaultAsync(predicate);
    }

    // write to in-memory repository
    public void Add(T item)
    {
        DbSet.Add(item);
    }

    // public async Task UpdateAsync(T item){
    //     var foundItem = await DbSet.FirstOrDefaultAsync(i => i.Guid == item.Guid)
    //                     ?? throw new ApplicationException($"Update failed, item not found");
    //     _dbContext.Entry(foundItem).CurrentValues.SetValues(item);
    //     _dbContext.Entry(foundItem).State = EntityState.Modified;
    // }

    // write to database
    public async Task<bool> SaveChangesAsync()
    {
        return await _dbContext.SaveAllChangesAsync();
    }

    public T? Attach(T item)
    {
        EntityEntry<T> entityEntry = _dbContext.Attach<T>(item);
        return entityEntry.Entity;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await SelectAsync();
    }

    public void AddRange(IEnumerable<T> items)
    {
        DbSet.AddRange(items);
    }
}