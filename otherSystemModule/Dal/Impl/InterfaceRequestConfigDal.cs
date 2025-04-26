using domain.Pojo.ortherSystems;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace otherSystemModule.Dal.Impl;


[Service]
public class InterfaceRequestConfigDal : IInterfaceRequestConfigDal
{
    private readonly ISqlSugarClient db;
    public InterfaceRequestConfigDal(DbClientFactory dbClientFactory)
    {
        this.db = dbClientFactory.db;
    }

    public void Insert(InterfaceRequestConfig config)
    {
        db.Insertable(config).ExecuteCommand();
    }

    public void Update(InterfaceRequestConfig config)
    {
        db.Updateable(config).ExecuteCommand();
    }

    public void Delete(InterfaceRequestConfig config)
    {
        db.Deleteable(config).ExecuteCommand();
    }

    public void DeleteById(List<long> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }
        db.Deleteable<InterfaceRequestConfig>().In(ids).ExecuteCommand();
    }

    public InterfaceRequestConfig SelectById(long id)
    {
        return db.Queryable<InterfaceRequestConfig>().Single(i => i.configId == id && i.isDelete == false);
    }
}