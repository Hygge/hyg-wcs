using domain.Dto;
using domain.Enums;
using domain.Pojo.protocol;
using domain.Records;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Exceptions;
using infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace dataPointsModule.Dal.Impl;


[Service]
public class OpcUaDataPointDal : IOpcUaDataPointDal
{
    private readonly ILogger<IOpcUaDataPointDal> _logger;
    private readonly ISqlSugarClient db;

    public OpcUaDataPointDal(ILogger<IOpcUaDataPointDal> _logger, DbClientFactory _dbClientFactory)
    {
        this._logger = _logger;
        this.db = _dbClientFactory.db;
    }

    public void DeleteBatchById(List<long> ids)
    {
        db.Deleteable<OpcUaDataPoint>().In(ids).ExecuteCommand();
    }

    public void Insert(OpcUaDataPoint opcUaDataPoint)
    {
        db.Insertable(opcUaDataPoint).ExecuteCommand();
    }

    public void Update(OpcUaDataPoint opcUaDataPoint)
    {
        db.Updateable(opcUaDataPoint).ExecuteCommand();
    }

    public void DeleteById(long opcUaDataPointId)
    {
        db.Deleteable<OpcUaDataPoint>().Where( o => o.id == opcUaDataPointId).ExecuteCommand();
    }

    public OpcUaDataPoint SelectById(long opcUaDataPointId)
    {
        return db.Queryable<OpcUaDataPoint>().Single(o => o.id == opcUaDataPointId);
    }

    public OpcUaDataPoint SelectByNameAndOperate(string name, OperateEnum operate)
    {
        return db.Queryable<OpcUaDataPoint>().Where(o => o.name.Equals(name) && o.operate == operate).Single();
    }

    public Pager<OpcUaDataPoint> SelectList(OpcUaDataPointQuery query)
    {
        var exp = Expressionable.Create<OpcUaDataPoint>();
        exp.AndIF(!string.IsNullOrEmpty(query.name), e => e.name.Contains(query.name));
        exp.AndIF(!string.IsNullOrEmpty(query.category), e => e.category.Contains(query.category));
        exp.AndIF(!string.IsNullOrEmpty(query.identifier), e => e.identifier.Contains(query.identifier));
        exp.AndIF(!string.IsNullOrEmpty(query.ip), e => e.endpoint.Contains(query.ip));

        Pager<OpcUaDataPoint> pager = new(query.pageNum, query.pageSize);

        int total = 0;
        pager.rows = db.Queryable<OpcUaDataPoint>().Where(exp.ToExpression())
            .ToPageList(query.pageNum, query.pageSize, ref total);
        pager.total = total;
        
        return pager;
    }

    public List<string> SelectEndpoints()
    {
        List<string> list = db.Queryable<OpcUaDataPoint>().Select<string>(o => o.endpoint).Distinct().ToList();
        return list;
        
    }

    public void InsertBacth(List<OpcUaDataPoint> list)
    {
        db.Ado.BeginTran();
        try
        {
            db.Insertable(list).ExecuteCommand();

            db.Ado.CommitTran();
        }
        catch (Exception ex)
        {
            db.Ado.RollbackTran();
            throw new BusinessException($"batch insert failed, reason:{ex.Message}");
        }
        
    }

    public List<OpcUaPointDto> SelectAll(OpcUaDataPointQuery query)
    {
        var exp = Expressionable.Create<OpcUaDataPoint>();
        exp.AndIF(!string.IsNullOrEmpty(query.name), e => e.name.Contains(query.name));
        exp.AndIF(!string.IsNullOrEmpty(query.category), e => e.category.Contains(query.category));
        exp.AndIF(!string.IsNullOrEmpty(query.identifier), e => e.identifier.Contains(query.identifier));
        exp.AndIF(!string.IsNullOrEmpty(query.ip), e => e.endpoint.Contains(query.ip));

       return db.Queryable<OpcUaDataPoint>().Where(exp.ToExpression())
           .Select<OpcUaPointDto>( item => new OpcUaPointDto()
           {
               name = item.name,
               category = item.category,
               accessType = item.accessType,
               dataType = item.dataType,
               endpoint = item.endpoint,
               identifier = item.identifier,
               namespaceIndex = item.namespaceIndex,
               remark = item.remark,
               operate = item.operate,
           })
            .ToList();
    }
}