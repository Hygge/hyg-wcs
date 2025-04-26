using domain.Dto;
using domain.Pojo.protocol;
using domain.Records;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Utils;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace dataPointsModule.Dal.Impl;

[Service]
public class ModbusDataDal : IModbusDataDal
{
    private readonly ILogger<IModbusDataDal> _logger;
    private readonly ISqlSugarClient db;
    
    public ModbusDataDal(DbClientFactory dbClientFactory, ILogger<IModbusDataDal> _logger)
    {
        this.db = dbClientFactory.db;
        this._logger = _logger;
    }
    public Pager<ModbusDataPoint> pager(ModbusDataQuery query)
    {
        Pager<ModbusDataPoint> pager = new(query.pageNum, query.pageSize);
        var exp = Expressionable.Create<ModbusDataPoint>();
        exp.AndIF(!string.IsNullOrEmpty(query.name), m => m.name.Contains(query.name));
        exp.AndIF(!string.IsNullOrEmpty(query.category), m => m.category.Contains(query.category));
        exp.AndIF(!string.IsNullOrEmpty(query.ip), m => m.category.Contains(query.ip));
        exp.AndIF(query.startAddress is not null, m => m.startAddress == query.startAddress);

        pager.rows = db.Queryable<ModbusDataPoint>().Where(exp.ToExpression()).Skip(pager.getSkip()).Take(pager.pageSize).ToList();
        pager.total = db.Queryable<ModbusDataPoint>().Where(exp.ToExpression()).Count();

        return pager;
    }

    public void Insert(ModbusDataPoint modbusDataPoint)
    {
        db.Insertable<ModbusDataPoint>(modbusDataPoint).ExecuteCommand();
    }

    public ModbusDataPoint SelectByName(string name)
    {
        return db.Queryable<ModbusDataPoint>().Single(x => x.name.Equals(name));
    }

    public void Delete(List<long> ids)
    {
        db.Deleteable<ModbusDataPoint>().In(ids).ExecuteCommand();
    }

    public ModbusDataPoint SelectById(long id)
    {
        return db.Queryable<ModbusDataPoint>().First(x => x.id == id);
    }

    public void BatchInsert(List<ModbusDataPoint> points)
    {
        db.Insertable(points).ExecuteCommand();
    }

    public List<ModbusPointDto> SelectList(ModbusDataQuery query)
    {
        var exp = Expressionable.Create<ModbusDataPoint>();
        exp.AndIF(!string.IsNullOrEmpty(query.name), m => m.name.Contains(query.name));
        exp.AndIF(!string.IsNullOrEmpty(query.category), m => m.category.Contains(query.category));
        exp.AndIF(!string.IsNullOrEmpty(query.ip), m => m.category.Contains(query.ip));
        exp.AndIF(query.startAddress is not null, m => m.startAddress == query.startAddress);

        return db.Queryable<ModbusDataPoint>().Where(exp.ToExpression())
            .Select(item => new ModbusPointDto()
            {
                name = item.name,
                category = item.category,
                ip = item.ip,
                port = item.port,
                dataType = item.dataType,
                startAddress = item.startAddress,
                stationNo = item.stationNo,
                length = item.length,
                readOnly = item.readOnly,
                format = item.format,
                remark = item.remark
            }).ToList();
    }

    public void Update(ModbusDataPoint point)
    {
        db.Updateable(point).ExecuteCommand();
    }
}