using System.Collections.Concurrent;
using System.Reflection;
using SqlSugar;

namespace infrastructure.Db;

public static class  SqlSugarExtensions
{
    /// <summary>
    /// 批量插入实体类并返回主键自增id至实体类
    /// </summary>
    /// <param name="db"></param>
    /// <param name="entities"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public static async Task InsertBatchReturnIdentityIntoEntity<TEntity, TKey>(this ISqlSugarClient db, List<TEntity> entities) 
        where TEntity : class, new()
    {
        if (entities is null || entities.Count == 0)
            return;
        var identityProperty = CacheIdentityProperty(typeof(TEntity));
        if (identityProperty == null)
        {
            throw new InvalidOperationException(
                $"实体 {typeof(TEntity).Name} 未找到标记为[IsIdentity]和[IsPrimaryKey]的自增主键属性");
        }
        List<TKey> ids = await db.Insertable(entities).ExecuteReturnPkListAsync<TKey>();
        
        Parallel.For(0, ids.Count, i =>
        {
            identityProperty.SetValue(entities[i], ids[i]);
        });
    }
    // 缓存自增主键属性（提升性能）
    private static readonly ConcurrentDictionary<Type, PropertyInfo> 
        IdentityPropertyCache = new ConcurrentDictionary<Type, PropertyInfo>();
    private static PropertyInfo? CacheIdentityProperty(Type entityType)
    {
        return IdentityPropertyCache.GetOrAdd(entityType, type => 
        {
            return type.GetProperties()
                .FirstOrDefault(prop => 
                {
                    var attr = prop.GetCustomAttribute<SugarColumn>();
                    return attr != null && attr.IsIdentity && attr.IsPrimaryKey;
                });
        });
    }
}
