using domain.Pojo.sys;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Utils;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace adminModule.Bll.Impl;


[Service]
public class SysLogBll : ISysLogBll
{
    private readonly ILogger<ISysLogBll> _logger;
    private readonly ISqlSugarClient db;

    public SysLogBll(ILogger<ISysLogBll> logger, DbClientFactory _dbClientFactory)
    {
        this._logger = logger;
        this.db = _dbClientFactory.db;
    }

    
    
    public void Save(SysLog log)
    {
        db.Insertable(log).ExecuteCommand();
    }

    public Pager<SysLog> GetList(string? path, string? operation, string? operatorName, DateTime? startTime, DateTime? endTime,
        int pageNum, int pageSize)
    {
        Pager<SysLog> pager = new(pageNum, pageSize);
        var exp = Expressionable.Create<SysLog>();
        exp.AndIF(!string.IsNullOrEmpty(path), x => x.path.Contains(path));
        exp.AndIF(!string.IsNullOrEmpty(operation), x => x.operation.Contains(operation));
        exp.AndIF(!string.IsNullOrEmpty(operatorName), x => x.operatorName.Contains(operatorName));
        exp.AndIF( null != startTime, x => x.operateTime >= startTime);
        exp.AndIF( null != endTime, x => x.operateTime <= endTime);
        
        int total = 0;
        pager.rows = db.Queryable<SysLog>().Where(exp.ToExpression()).OrderByDescending(x => x.operateTime)
            .ToPageList(pageNum, pageSize, ref total);
        pager.total = total;

        return pager;
    }

    public void Delete(List<long> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }
        db.Deleteable<SysLog>().In(ids).ExecuteCommand();
    }
}