using domain.Pojo.ortherSystems;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Yitter.IdGenerator;

namespace otherSystemModule.Bll.Impl;

[Service]
public class OtherSysLogBll : IOtherSysLogBll
{
    private readonly ILogger<IOtherSysLogBll> _logger;
    private readonly ISqlSugarClient db;

    public OtherSysLogBll(ILogger<IOtherSysLogBll> _logger, DbClientFactory _dbClientFactory)
    {
        this._logger = _logger;
        this.db = _dbClientFactory.db;
    }
    
    
    public void Save(OtherSysLog otherSysLog)
    {
        otherSysLog.id = YitIdHelper.NextId();
        db.Insertable<OtherSysLog>(otherSysLog).ExecuteCommand();

    }

    public void Delete(List<long> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }
        db.Deleteable<OtherSysLog>().In(ids).ExecuteCommand();
    }

    public Pager<OtherSysLog> GetList(string? path, bool? executeStatus, string? sysName, DateTime? startTime, DateTime? endTime, int pageNum,
        int pageSize)
    {
        Pager<OtherSysLog> pager = new(pageNum, pageSize);
        var exp = Expressionable.Create<OtherSysLog>();
        exp.AndIF(!string.IsNullOrEmpty(path), x => x.sysName.Contains(path));
        exp.AndIF(!string.IsNullOrEmpty(sysName), x => x.sysName.Contains(sysName));
        exp.AndIF(null != executeStatus, x => x.executeStatus == executeStatus);
        exp.AndIF( null != startTime, x => x.operateTime >= startTime);
        exp.AndIF( null != endTime, x => x.operateTime <= endTime);
        
        pager.total = db.Queryable<OtherSysLog>().Where(exp.ToExpression()).Count();
        pager.rows = db.Queryable<OtherSysLog>().Where(exp.ToExpression()).OrderByDescending(x => x.operateTime)
            .Skip(pager.getSkip()).Take(pager.pageSize)
            .ToList();
        return pager;
    }
}