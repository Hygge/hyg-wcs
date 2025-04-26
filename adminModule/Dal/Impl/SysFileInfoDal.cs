using domain.Enums;
using domain.Pojo.sys;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace adminModule.Dal.Impl;

[Service]
public class SysFileInfoDal : ISysFileInfoDal
{

    private readonly ISqlSugarClient db;
    public SysFileInfoDal(DbClientFactory dbClientFactory)
    {
        db = dbClientFactory.db;
    }
    
    
    public void Insert(SysFileInfo entity)
    {
        db.Insertable<SysFileInfo>(entity).ExecuteCommand();
    }

    public Pager<SysFileInfo> Select(int? type, int skip, int take)
    {
        Pager<SysFileInfo> pager = new(skip, take);
        var exp = Expressionable.Create<SysFileInfo>();
        exp.AndIF(true, s => s.isDelete == true);
        exp.AndIF(type != null, f => f.fileType == (FileTypeEnum)type);

        pager.rows = db.Queryable<SysFileInfo>().Where(exp.ToExpression()).OrderByDescending(d => d.createdTime)
            .Skip(pager.getSkip()).Take(pager.pageSize).ToList();
        pager.total = db.Queryable<SysFileInfo>().Where(exp.ToExpression()).Count();
        return pager;
    }
}