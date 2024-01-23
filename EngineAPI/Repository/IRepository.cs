using EngineAPI.Domain.DataModels;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Linq.Expressions;


namespace EngineAPI.Repository;

public interface IRepository
{
    Task<T> FindById<T>(int id) where T : class;
    Task<IEnumerable<T>> FindAll<T>(bool trackChanges) where T : class;
    Task<IEnumerable<T>> FindByCondition<T>(Expression<Func<T, bool>> expression, bool trackChanges) where T : class;
    Task<T> Create<T>(T entity) where T : class;
    Task<T> Update<T>(T entity) where T : class;
    Task<T> Delete<T>(int id) where T : class;
    Task<IDbContextTransaction> StartTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    void Save();
    Task<List<TEntity>> BulkSave<TEntity>(List<TEntity> entities) where TEntity : class;

    Task<List<VwIndicesView>> FindDocuments(HashSet<string> concepts, HashSet<string?> instances, string filter);
}
