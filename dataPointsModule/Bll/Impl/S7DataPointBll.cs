
using dataPointsModule.Managers;
using domain.Dto;
using domain.Enums;
using domain.Pojo.protocol;
using domain.Records;
using domain.Result;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Exceptions;
using infrastructure.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using S7.Net;
using SqlSugar;

namespace dataPointsModule.Bll.Impl;

[Service]
public class S7DataPointBll : IS7DataPointBll
{
    private readonly ILogger<S7DataPointBll> _logger;
    private readonly DbClientFactory _dbClientFactory = ServiceUtil.GetRequiredService<DbClientFactory>() ;
    private readonly IS7Manager _manager  = ServiceUtil.GetRequiredService<IS7Manager>() ;

    public S7DataPointBll(ILogger<S7DataPointBll> logger)
    {
        this._logger = logger;
    }

    private record Device(string ip, int port);

    public void Initializes()
    {
        using var db = _dbClientFactory.GetSqlSugarClient();
        List<Device> sDevices = db.Queryable<S7DataPoint>().Select<Device>(x => new Device(x.ip, x.port)).Distinct().ToList();
        List<S7DataPoint> list = new();
        sDevices.ForEach(item =>
        {
            S7DataPoint? s7 = db.Queryable<S7DataPoint>().First(x => x.ip.Equals(item.ip) && x.port == item.port);
            if (s7 != null)
            {
                list.Add(s7);
            }
        });
        if (list.Count == 0)
        {
            return;
        }
        list.ForEach(item =>
        {
            _manager.Connect(item);
        });
    }

   
    public Pager<S7DataPoint> GetList(S7DataPointQuery query)
    {
        Pager<S7DataPoint> pager = new(query.pageNum, query.pageSize);
        var exp = Expressionable.Create<S7DataPoint>();
        exp.AndIF(!string.IsNullOrEmpty(query.name), x => x.name.Contains(query.name));
        exp.AndIF(!string.IsNullOrEmpty(query.name), x => x.category.Contains(query.category));
        exp.AndIF( query.startAddress != null, x => x.startAddress == query.startAddress);
        using var db = _dbClientFactory.GetSqlSugarClient();
        pager.rows = db.Queryable<S7DataPoint>().Where(exp.ToExpression()).OrderByDescending(s => s.id).Skip(pager.getSkip())
            .Take(pager.pageSize).ToList();
        pager.total = db.Queryable<S7DataPoint>().Where(exp.ToExpression()).Count();
        return pager;
    }


    public void Save(S7DataPoint point)
    {
        using var db = _dbClientFactory.GetSqlSugarClient();
        S7DataPoint? s7DataPoint = db.Queryable<S7DataPoint>().Where(x => x.name.Equals(point.name)).First();
        if (s7DataPoint != null)
        {
            throw new BusinessException("数据点名称已存在");
        }
        db.Insertable(point).ExecuteCommand();
    }

    public void Del(List<long> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }
        using var db = _dbClientFactory.GetSqlSugarClient();
        db.Deleteable<S7DataPoint>().In(ids).ExecuteCommand();
    }

    public void Update(S7DataPoint point)
    {
        using var db = _dbClientFactory.GetSqlSugarClient();
        S7DataPoint s7point = db.Queryable<S7DataPoint>().First(x => x.id == point.id);
        if (s7point is null)
        {
            throw new BusinessException(HttpCode.FAILED_CODE, "数据点不存在");
        }
        db.Updateable(point).ExecuteCommand();
    }

    public S7DataPoint GetById(long id)
    {
        using var db = _dbClientFactory.GetSqlSugarClient();
        return db.Queryable<S7DataPoint>().First(x => x.id == id);
    }

    public List<S7DataPoint> GetAll()
    {
        using var db = _dbClientFactory.GetSqlSugarClient();
        return db.Queryable<S7DataPoint>().ToList();
    }

    public void BatchSave(List<S7DataPoint> points)
    {
        using var db = _dbClientFactory.GetSqlSugarClient();
        db.Ado.BeginTran();
        try
        {
            db.Insertable<S7DataPoint>(points).ExecuteCommand();
            db.Ado.CommitTran();
        }
        catch (Exception ex)
        {
            db.Ado.RollbackTran();
            throw new BusinessException(500, "批量保存数据点失败" + ex.Message);
        }
    }

    public DataPointDto ReadByName(string name)
    {
        using var db = _dbClientFactory.GetSqlSugarClient();
        S7DataPoint? point = db.Queryable<S7DataPoint>()
            .Where(x => name.Equals(x.name) && x.operate == OperateEnum.Read).First();
        if (point is null)
        {
            throw new BusinessException(HttpCode.FAILED_CODE, "数据点不存在");
        }
        return _manager.Read(point);
    }

    public DataPointDto WriteByName(string name, object value)
    {
        using var db = _dbClientFactory.GetSqlSugarClient();
        S7DataPoint? point = db.Queryable<S7DataPoint>()
            .Where(x => name.Equals(x.name) && x.operate == OperateEnum.Write).First();
        if (point is null)
        {
            throw new BusinessException(HttpCode.FAILED_CODE, "数据点不存在");
        }
        return _manager.Write(point, value);
    }

    public FileStreamResult ExportExcel(S7DataPointQuery query)
    {
        try
        {
            var exp = Expressionable.Create<S7DataPoint>();
            exp.AndIF(!string.IsNullOrEmpty(query.name), x => x.name.Contains(query.name));
            exp.AndIF(!string.IsNullOrEmpty(query.name), x => x.category.Contains(query.category));
            exp.AndIF( query.startAddress != null, x => x.startAddress == query.startAddress);
            using var db = _dbClientFactory.GetSqlSugarClient();
            List<S7PointDto> list = db.Queryable<S7DataPoint>()
                .Where(exp.ToExpression())
                .Select(item => new S7PointDto()
                {
                    name = item.name,
                    category = item.category,
                    ip = item.ip,
                    cpuType = item.cpuType,
                    rack = item.rack,
                    slot = item.slot,
                    db = item.db,
                    startAddress = item.startAddress,
                    dataType = item.dataType,
                    length = item.length,
                    remark = item.remark,
                    operate = item.operate,
                })
                .ToList();
            var memoryStream = new MemoryStream();
            memoryStream.SaveAs(list);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = "S7数据点.xlsx"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,$"s7数据点导出Excel失败, 原因：{ex.Message}");
            throw new BusinessException("s7数据点导出Excel失败");
        }
    }

    public void importExcel(IFormFile file)
    {
        FileTypeEnum fileType = FileUtil.GetIFormFileType(Path.GetExtension(file.FileName));
        if (FileTypeEnum.Excel != fileType)
        {
            throw new BusinessException("文件类型异常，请选择excel文件");
        }

        List<S7DataPoint> list = new();
        var rows = MiniExcel.Query<S7PointDto>(file.OpenReadStream());
        foreach (var dto in rows.ToList())
        {
            S7DataPoint point = new();
            point.name = dto.name;
            point.category = dto.category;
            point.ip = dto.ip;
            point.cpuType = dto.cpuType;
            point.rack = dto.rack;
            point.slot = dto.slot;
            point.dataType = dto.dataType;
            point.db = dto.db;
            point.startAddress = dto.startAddress;
            point.length = dto.length;
            point.remark = dto.remark;
            point.operate = dto.operate;
            list.Add(point);
        }
        BatchSave(list);
    }
}