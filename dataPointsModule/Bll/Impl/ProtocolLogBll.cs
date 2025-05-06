using domain.Enums;
using domain.Pojo.protocol;
using domain.Records;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace dataPointsModule.Bll.Impl;

[Service]
public class ProtocolLogBll : IProtocolLogBll
{
    private readonly ILogger<IProtocolLogBll> _logger;
    private readonly ISqlSugarClient db;

    public ProtocolLogBll(ILogger<IProtocolLogBll> _logger, DbClientFactory _dbClientFactory)
    {
        this._logger = _logger;
        this.db = _dbClientFactory.db;
    }

    public void Save(ProtocolLog protocolLog)
    {
        ProtocolLog? p = db.Queryable<ProtocolLog>().Where(p => p.name.Equals(protocolLog.name) 
                                                                && p.oper.Equals(OperateEnum.Read.ToString()))
            .OrderByDescending(p => p.createdTime).First();
        if (p is not null)
        {
            if(p.status == protocolLog.status && p.value.Equals(protocolLog.value))
                return; // 读取数据与最新记录状态一致时忽略
            db.Insertable<ProtocolLog>(protocolLog).ExecuteCommand();
        }

        else
            db.Insertable<ProtocolLog>(protocolLog).ExecuteCommand();
    }

    public void Delete(List<long> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }
        db.Deleteable<ProtocolLog>().In(ids).ExecuteCommand();

    }

    public Pager<ProtocolLog> GetList(ProtocolLogQuery query)
    {
        Pager<ProtocolLog> pager = new(query.pageNum, query.pageSize);
        var exp = Expressionable.Create<ProtocolLog>();
        exp.AndIF(!string.IsNullOrEmpty(query.name), x => x.name.Contains(query.name));
        exp.AndIF(!string.IsNullOrEmpty(query.category), x => x.category.Equals(query.category));
        exp.AndIF(query.status != null, x => x.status == query.status);
        exp.AndIF(!string.IsNullOrEmpty(query.oper), x =>  query.oper.Equals(x.oper));
        exp.AndIF(query.startTime != null, x => x.endTime >= query.startTime);
        exp.AndIF(query.endTime != null, x => x.endTime <= query.endTime);

        int total = 0;
        List<ProtocolLog> list = db.Queryable<ProtocolLog>().Where(exp.ToExpression())
            .OrderByDescending(x => x.endTime)
            .ToPageList(query.pageNum, query.pageSize, ref total);
        pager.total = total;
        pager.rows = list;
        return pager;
    }
}