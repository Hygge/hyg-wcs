using domain.Enums;
using domain.Pojo.sys;
using domain.Vo;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Yitter.IdGenerator;

namespace adminModule.Bll.Impl
{

    [Service]
    public class SysRoleBll : ISysRoleBll
    {
        private readonly ISqlSugarClient db;
        private readonly ILogger<SysRoleBll> _logger;

        public SysRoleBll(ILogger<SysRoleBll> logger, DbClientFactory dbClientFactory)
        {
            this.db = dbClientFactory.db;
            this._logger = logger;
        }


        public List<SysRole> GetList(string? roleName, string? key, int? status, DateTime? start, DateTime? end)
        {
            var exp = Expressionable.Create<SysRole>();
            exp.OrIF(!string.IsNullOrEmpty(roleName), it => it.roleName.Equals(roleName));
            exp.AndIF(key != null, item => item.roleKey.Equals(key));
            exp.AndIF(status != null, item => item.disabled == status);
            exp.AndIF(start != null, item => item.createdTime >= start);
            exp.AndIF(start != null, item => item.createdTime <= end);

            // 执行查询
            return db.Queryable<SysRole>().Where(exp.ToExpression()).ToList();

        }

        public void Add(string name, string key, string remark, int? status, string createBy)
        {
            SysRole sysRole = new SysRole();
            sysRole.id = YitIdHelper.NextId();
            sysRole.roleName = name;
            sysRole.roleKey = key;
            sysRole.remark = remark;
            sysRole.disabled = status == null ? 0 : (int)status;
            sysRole.createdBy = createBy;

            SysRole? s = db.Queryable<SysRole>().Where(x => x.roleName.Equals(name) || x.roleKey.Equals(key)).Single();
            if (s != null)
            {
                throw new BusinessException(424, "角色名称或角色key存在！");
            }
            db.Insertable<SysRole>(sysRole).ExecuteCommand();
        }

        public void Delete(long id)
        {
            db.Deleteable<SysRole>().Where(it => it.id == id).ExecuteCommand();
        }

        public void Update(SysRole role)
        {
            try
            {
                // 执行更新操作，这里使用UpdateOneAsync，因为它会更新找到的第一条记录
                var result = db.Updateable<SysRole>(role)
                .SetColumns(x => x.remark, role.remark)
                .SetColumns(x => x.disabled, role.disabled).Where(x => x.id == role.id).ExecuteCommand();

                if (result > 0)
                {
                    Console.WriteLine("文档更新成功。");
                }
                else
                {
                    Console.WriteLine("没有匹配的文档被更新。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新操作失败: {ex.Message}");
                throw new BusinessException(ex.Message);
            }

        }

        public void AddUserAndRole(List<long>? roleIds, long userId)
        {
            List<SysUserAndRole> list = new();
            roleIds?.ForEach(item =>
            {
                SysRole? singleOrDefault = db.Queryable<SysRole>().Where(x => x.id == item).Single();
                if (singleOrDefault != null)
                {
                    SysUserAndRole sysUserAndRole = new SysUserAndRole();
                    sysUserAndRole.userId = userId;
                    sysUserAndRole.roleId = item;
                    list.Add(sysUserAndRole);
                }
            });
            if (list.Count > 0)
            {
                db.Insertable<SysUserAndRole>(list).ExecuteCommand();
            }
        }

        public void AddPermissons(long roleId, List<long> menuIds)
        {
            List<SysRoleAndMenu> list = new List<SysRoleAndMenu>();
            foreach (var menuId in menuIds)
            {
                if (0 == menuId)
                {
                    continue;
                }
                SysRoleAndMenu roleAndMenu = new SysRoleAndMenu();
                roleAndMenu.roleId = roleId;
                roleAndMenu.menuId = menuId;
                list.Add(roleAndMenu);
            }

            if (list.Count > 0)
            {
                db.Deleteable<SysRoleAndMenu>().Where(rm => rm.roleId == roleId).ExecuteCommand();
                db.Insertable<SysRoleAndMenu>(list).ExecuteCommand();
            }
        }

        public void BindUserRole(long id, List<long> roleIds)
        {
            List<SysUserAndRole> list = new();
            roleIds?.ForEach(item =>
            {
                SysRole? singleOrDefault = db.Queryable<SysRole>().Where(x => x.id == item).Single();
                if (singleOrDefault != null)
                {
                    SysUserAndRole sysUserAndRole = new SysUserAndRole();
                    sysUserAndRole.userId = id;
                    sysUserAndRole.roleId = item;
                    list.Add(sysUserAndRole);
                }
            });
            if (list.Count > 0)
            {
                db.Deleteable<SysUserAndRole>().Where(x => x.userId == id).ExecuteCommand();
                db.Insertable<SysUserAndRole>(list).ExecuteCommand();
            }
        }

        public void RoleBindUser(long roleId, List<long> userIds)
        {
            SysRole? role = db.Queryable<SysRole>().Single(x => x.id == roleId);
            if (null != role)
            {
                // 删除原来绑定记录
                db.Deleteable<SysUserAndRole>().Where(x => x.roleId == roleId).ExecuteCommand();
                List<SysUserAndRole> list = new List<SysUserAndRole>();
                userIds.ForEach(item =>
                {
                    SysUser? sysUser = db.Queryable<SysUser>().Single(u => u.id == item);
                    if (null != sysUser)
                    {
                        SysUserAndRole sysUserAndRole = new SysUserAndRole();
                        sysUserAndRole.userId = item;
                        sysUserAndRole.roleId = roleId;
                        list.Add(sysUserAndRole);
                    }
                });
                if (list.Count > 0)
                {
                    db.Insertable<SysUserAndRole>(list).ExecuteCommand();
                }
            }
        }

        public List<string> GetPermissons(long userId)
        {
            return GetPerMenuVos(userId).Select( x => x.pem).ToList();
        }

        public List<long> GetPermissonsMenuIdsByRole(long roleId)
        {
            var query = db.Queryable<SysRoleAndMenu>()
                .Where(rm => rm.roleId == roleId)
                .Select( rm => rm.menuId);
            return query.ToList();
        }

        public List<long> GetRoleBindUserIds(long id)
        {
            List<long> list = db.Queryable<SysUserAndRole>().Where(x => x.roleId == id)
                .Select(x => x.userId).ToList();
            return list;
        }

        private List<SysMenuVo> GetPerMenuVos(long userId)
        {
            var query = db.Queryable<SysUserAndRole>().Where(ur => ur.userId == userId)
                .LeftJoin<SysRole>((ur, r) => r.id == ur.roleId).Where( (ur, r) => r.disabled == (int)DisabledEnum.ZERO)
                .LeftJoin<SysRoleAndMenu>((ur, r, rm) => rm.roleId == r.id)
                .LeftJoin<SysMenu>((ur, r, rm, m1) => m1.id == rm.menuId)
                .Distinct()
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
                    order = m1.order
                });

            return query.ToList();
        }
        
        
    }
}
