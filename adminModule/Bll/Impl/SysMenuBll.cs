
using domain.Dto;
using domain.Enums;
using domain.Pojo.sys;
using domain.Vo;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Yitter.IdGenerator;

namespace adminModule.Bll.Impl;


[Service]
public class SysMenuBll : ISysMenuBll
{

    private readonly ILogger<ISysMenuBll> _logger;
    private readonly ISqlSugarClient db;

    public SysMenuBll(ILogger<ISysMenuBll> logger, DbClientFactory _dbClientFactory)
    {
        this._logger = logger;
        this.db = _dbClientFactory.db;
    }



    public List<SysMenuVo> GetList(int? status, string? title)
    {
        var exp = Expressionable.Create<SysMenu>();
        exp.AndIF(!string.IsNullOrEmpty(title), x => x.title.Contains(title));
        exp.AndIF(null != status, x => x.enable == status);
    
        List<SysMenuVo> sysMenus = db.Queryable<SysMenu>().Where(exp.ToExpression()).OrderBy(x => x.order)
            .Select( m1 =>                 
                new SysMenuVo()
                {
                    id = m1.id,
                    title = m1.title,
                    enable = m1.enable,
                    icon = m1.icon,
                    parentId = m1.parentId,
                    pem = m1.pem,
                    route = m1.route,
                    type = m1.type,
                    path = m1.path,
                    keepAlive = m1.keepAlive,
                    key = m1.id,
                    show = m1.show,
                    query = m1.query,
                    target = m1.target,
                    order = m1.order
                }                   
            )
            .ToList();           
        // 生成树形菜单返回
        List<SysMenuVo> list = new List<SysMenuVo>();
        BuildTree(sysMenus, list);
        return list;
    }

    public List<SysMenuVo> GetTree(long id, string? title)
    {
        List<SysUserAndRole> sysUserAndRole = db.Queryable<SysUserAndRole>().Where(x => x.userId == id).ToList();
        if (null == sysUserAndRole || sysUserAndRole.Count == 0)
        {
            throw new BusinessException(404, "该用户未绑定角色，暂无菜单");
        }

        var query = db.Queryable<SysUserAndRole>().Where(ur => ur.userId == id)
            .LeftJoin<SysRole>((ur, r) => r.id == ur.roleId).Where((ur, r) => r.disabled == (int)DisabledEnum.ZERO)
            .LeftJoin<SysRoleAndMenu>((ur, r, rm) => rm.roleId == r.id)
            .LeftJoin<SysMenu>((ur, r, rm, m1) => m1.id == rm.menuId)
           ;
        if (!string.IsNullOrEmpty(title))
        {
            query = query.Where((ur, r, rm, m1) => m1.title.Contains(title));
        }
        List<SysMenuVo> sysMenus = query.Distinct()
            .Select((ur, r, rm, m1) => new SysMenuVo()
            {
                id = m1.id,
                title = m1.title,
                enable = m1.enable,
                icon = m1.icon,
                parentId = m1.parentId,
                pem = m1.pem,
                route = m1.route,
                type = m1.type,
                path = m1.path,
                keepAlive = m1.keepAlive,
                key = m1.id,
                show = m1.show,
                target = m1.target,
                query = m1.query,
                order = m1.order
            }).ToList();    
        // 生成树形菜单返回
        List<SysMenuVo> list = new List<SysMenuVo>();
        BuildTree(sysMenus, list);
        return list;
    }

    private void BuildTree(List<SysMenuVo> source, List<SysMenuVo> target)
    {
        source.ForEach(item =>
        {
            if (0 == item.parentId)
            {
                FindSubTree(item, source);
                target.Add(item);
            }
        });
    }

    private void FindSubTree(SysMenuVo vo, List<SysMenuVo> list)
    {
        List<SysMenuVo> childlist = new();
        list.ForEach(item =>
        {
            if (vo.id == item.parentId)
            {
                childlist.Add(item);
            }
        });
        if (childlist.Count == 0)
            return;
        vo.children.AddRange(childlist);
        childlist.ForEach(item => FindSubTree(item, list));
    }


    public void AddMenu(SysMenuDto sysMenuDto)
    {
        try{
            SysMenu sysMenu = new SysMenu();
            sysMenu.id = YitIdHelper.NextId();
            sysMenu.type = sysMenuDto.type;
            sysMenu.key = "";
            sysMenu.parentId = sysMenuDto.parentId;
            sysMenu.createdTime = DateTime.Now;
        
            sysMenu.title = sysMenuDto.title;
            sysMenu.icon = sysMenuDto.icon;
            sysMenu.type = sysMenuDto.type;
            sysMenu.order = sysMenuDto.order;
            sysMenu.target = (int)sysMenuDto.target;
            sysMenu.show = (int)sysMenuDto.show;
            sysMenu.enable = sysMenuDto.enable;

            sysMenu.route = sysMenuDto.route;
            sysMenu.path = sysMenuDto.path;
            sysMenu.keepAlive = sysMenuDto.keepAlive;
            sysMenu.pem = sysMenuDto.pem;
            sysMenu.query = sysMenuDto.query;
    
            db.Insertable<SysMenu>(sysMenu).ExecuteCommand();
        }catch(Exception exp){
            _logger.LogError(exp.Message);
            throw new BusinessException("菜单保存失败");
        }
        
    }

    public void UpdateMenu(SysMenuDto sysMenuDto)
    {
        SysMenu? menu = db.Queryable<SysMenu>().Where(x => x.id == sysMenuDto.id).Single();
        if (menu == null)
        {
            throw new BusinessException(424, "未知菜单修改失败");
        }

        var res = db.Updateable<SysMenu>()
            .SetColumns(x => x.title, sysMenuDto.title)
            .SetColumns(x => x.parentId, sysMenuDto.parentId)
            .SetColumns(x => x.route, sysMenuDto.route)
            .SetColumns(x => x.path, sysMenuDto.path)
            .SetColumns(x => x.target, sysMenuDto.target)
            .SetColumns(x => x.show, sysMenuDto.show)
            .SetColumns(x => x.query, sysMenuDto.query)
            .SetColumns(x => x.keepAlive, sysMenuDto.keepAlive)
            .SetColumns(x => x.icon, sysMenuDto.icon)
            .SetColumns(x => x.enable, sysMenuDto.enable)
            .SetColumns(x => x.pem, sysMenuDto.pem)
            .SetColumns(x => x.order, sysMenuDto.order).Where( x => x.id == menu.id).ExecuteCommand();

    }

    public void Delete(long id)
    {
        db.Deleteable<SysMenu>().Where(x => x.id == id).ExecuteCommand();
        db.Deleteable<SysRoleAndMenu>().Where(x => x.menuId == id).ExecuteCommand();

    }
}

