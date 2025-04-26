using domain.Enums;
using domain.Pojo.ortherSystems;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Exceptions;
using infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Yitter.IdGenerator;

namespace otherSystemModule.Bll.Impl;

[Service]
public class OtherSysInfoBll : IOtherSysInfoBll
{

    private readonly ILogger<OtherSysInfoBll> _logger;
    private readonly ISqlSugarClient db;
    
    public OtherSysInfoBll(DbClientFactory dbClientFactory, ILogger<OtherSysInfoBll> _logger)
    {
        this.db = dbClientFactory.db;
        this._logger = _logger;
    }
    
    public void Save(OtherSysInfo otherSysInfo)
    {
        if (string.IsNullOrEmpty(otherSysInfo.name))
        {
            throw new BusinessException(424, "系统名称不能为空");
        }
        OtherSysInfo? o = db.Queryable<OtherSysInfo>().First(x => x.name.Contains(otherSysInfo.name));
        if (null != o)
        {
            throw new BusinessException("系统名称已存在");
        }
        otherSysInfo.id = YitIdHelper.NextId();
        db.Insertable<OtherSysInfo>(otherSysInfo).ExecuteCommand();
    }

    public void IsPermissions(string? appKey, OtherSysLog otherSysLog)
    {
        if (string.IsNullOrEmpty(appKey))
        {
            otherSysLog.sysName = "未知系统";
            throw new BusinessException(401, "未知系统请联系管理员配置");
        }
        OtherSysInfo? otherSysInfo = db.Queryable<OtherSysInfo>().Single(x => x.appKey.Equals(appKey));
        if (otherSysInfo == null)
        {
            otherSysLog.sysName = "未知系统";
            throw new BusinessException(401, "未知系统请联系管理员配置");
        }
        otherSysLog.sysName = otherSysInfo.name;
        if ((int) DisabledEnum.ZERO != otherSysInfo.state)
        {
            
            throw new BusinessException(403, "当前系统不可以，请联系管理员打开");
        }
    }

    public void BuildAppkey(long id)
    {
        OtherSysInfo? otherSysInfo = db.Queryable<OtherSysInfo>().Single(x => x.id == id);
        if (otherSysInfo == null)
        {
            throw new BusinessException("未知其他系统，请先配置该系统再操作");
        }
        otherSysInfo.appKey = EncryptUtil.SHA256Encrypt(otherSysInfo.name + DateTimeOffset.Now.ToUnixTimeMilliseconds());
        db.Updateable(otherSysInfo).ExecuteCommand();
    }

    public void Delete(List<long> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }
        db.Deleteable<OtherSysInfo>().In(ids).ExecuteCommand();

    }

    public void Update(OtherSysInfo otherSysInfo)
    {
       OtherSysInfo? o = db.Queryable<OtherSysInfo>().Single(x => x.id == otherSysInfo.id);
       if (o == null)
       {
           throw new BusinessException("该系统不存在更新失败");
       }
       o.name = otherSysInfo.name;
       o.state = otherSysInfo.state;
        db.Updateable(o).ExecuteCommand();
        
    }

    public Pager<OtherSysInfo> GetList(string? sysName, DateTime? startTime, DateTime? endTime, int pageNum, int pageSize)
    {
        Pager<OtherSysInfo> pager = new(pageNum, pageSize);
        var exp = Expressionable.Create<OtherSysInfo>();
        exp.AndIF(!string.IsNullOrEmpty(sysName), x => x.name.Contains(sysName));
        exp.AndIF( null != startTime, x => x.createdTime >= startTime);
        exp.AndIF( null != endTime, x => x.createdTime <= endTime);
        
        pager.total = db.Queryable<OtherSysInfo>().Where(exp.ToExpression()).Count();
        pager.rows = db.Queryable<OtherSysInfo>().Where(exp.ToExpression()).OrderByDescending(x => x.createdTime)
            .Skip(pager.getSkip()).Take(pager.pageSize)
            .ToList();
        return pager;
    }
}