using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using EngineAPI.Domain.Data;
using System.Data;
using System.Diagnostics;
using EngineAPI.Domain.DataModels;

namespace EngineAPI.Repository;

public class Repository : IRepository
{
    private readonly IndexDBContext _context;
    public Repository(IndexDBContext context) => _context = context;

    public async Task<List<TEntity>> BulkSave<TEntity>(List<TEntity> entities) where TEntity : class
    {
        try
        {
            if (entities is null)
                throw new ArgumentNullException(nameof(entities));

            var dbset = _context.Set<TEntity>();
            await dbset.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
            return entities;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<T> Create<T>(T entity) where T : class
    {
        try
        {
            // set the incoming entity's id to 0
            entity.GetType().GetProperty("Id")?.SetValue(entity, 0);

            if (entity.GetType() == typeof(Activity))
            {
                entity.GetType().GetProperty("TimeStamp")?.SetValue(entity, DateTime.Now);
            }

            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<T> Delete<T>(int id) where T : class
    {
        try
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity != null)
            {
                _context.Entry(entity).State = EntityState.Deleted;
                await _context.SaveChangesAsync();
                return entity;
            }
            return entity;
        }
        catch (Exception x)
        {
            throw x;
        }
    }

    public async Task<IEnumerable<T>> FindAll<T>(bool trackChanges) where T : class
    {
        try
        {
            if (trackChanges)
            {
                return await _context.Set<T>().AsNoTracking().ToListAsync();
            }
            else
            {
                return await _context.Set<T>().ToListAsync();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }


    public async Task<IEnumerable<T>> FindByCondition<T>(Expression<Func<T, bool>> expression, bool trackChanges) where T : class
    {
        try
        {
            if (!trackChanges)
                return await _context.Set<T>().Where(expression).AsNoTracking().ToListAsync();
            else
                return await _context.Set<T>().Where(expression).ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<T> FindById<T>(int id) where T : class
    {
        try
        {
            return await _context.Set<T>().FindAsync(id);
        }
        catch (Exception) { throw; }
    }

    public async Task<List<VwIndicesView>> FindDocuments(HashSet<string> concepts, HashSet<string?> instances, string filter)
    {
        try
        {

            var result = await _context.VwIndicesViews
                .Where(item =>
                    concepts.Contains(item.ConceptDesc) &&
                    instances.Contains(item.Instance) &&
                    item.ParentConcept != filter)
                .GroupBy(item => item.Document)  
                .Select(group => group.First())  
                .ToListAsync();

            return result;

        }
        catch (Exception)
        {
            throw;
        }
    }

    public void Save() => _context.SaveChanges();

    public async Task<IDbContextTransaction> StartTransaction(IsolationLevel isolationLevel)
    {
        return await _context.Database.BeginTransactionAsync(isolationLevel);
    }

    public async Task<T> Update<T>(T entity) where T : class
    {
        try
        {
            _context.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.Single();
            entry.OriginalValues.SetValues(await entry.GetDatabaseValuesAsync());
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (Exception)
        {
            throw;
        }
    }
}
