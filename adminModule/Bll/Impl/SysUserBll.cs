
using domain.Dto;
using domain.Pojo.sys;
using domain.Records;
using domain.Vo;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Exceptions;
using infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Yitter.IdGenerator;

namespace adminModule.Bll.Impl
{
    [Service]
    public class SysUserBll : ISysUserBll
    {

        private readonly ISqlSugarClient db;
        private readonly ILogger<SysUserBll> _logger;
        private readonly IServiceProvider serviceProvider;

        public SysUserBll(ILogger<SysUserBll> logger, DbClientFactory dbClientFactory, IServiceProvider serviceProvider)
        {
            this.db = dbClientFactory.db;
            this._logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public long Login(string userName, string password)
        {
            SysUser? user1 = db.Queryable<SysUser>()
                .Single(item => item.userName.Equals(userName) && item.password.Equals(EncryptUtil.SHA256Encrypt(password)));
            if (null == user1)
            {
                throw new BusinessException("账号或密码错误");
            }

            return user1.id;

        }

        public void Add(SysUserDto userDto, string createdBy)
        {
            SysUser? sysUser = db.Queryable<SysUser>().Where(x => x.userName.Equals(userDto.userName)).Single();
            if (null != sysUser)
            {
                throw new BusinessException(424, "用户名已存在");
            }
            SysUser user = new SysUser();
            user.id = YitIdHelper.NextId();
            user.userName = userDto.userName;
            user.password = EncryptUtil.SHA256Encrypt(userDto.password);
            user.disabled = userDto.disabled;
            user.phoneNumber = userDto.phoneNumber;
            user.remark = userDto.remark;
            user.createdBy = createdBy;
            user.createdTime = DateTime.Now;
            user.nickName = userDto.nickName;

            db.Insertable<SysUser>(user).ExecuteCommand();
            serviceProvider.GetService<ISysRoleBll>()?.AddUserAndRole(userDto.roleIds, user.id);


        }

        public Pager<SysUserVo> GetList(Dictionary<string, object> map)
        {
            Pager<SysUserVo> pager = new Pager<SysUserVo>((int)map["pageNumber"], (int)map["pageSize"]);

            var exp = Expressionable.Create<SysUser>();
            exp.AndIF(map["userName"] != null && !string.IsNullOrEmpty((string)map["userName"]),
                x => x.userName.Contains(Convert.ToString(map["userName"])));

            exp.AndIF(map["phone"] != null && !string.IsNullOrEmpty((string)map["phone"]),
                x => x.phoneNumber.Contains(Convert.ToString(map["phone"])));

            exp.AndIF(map["status"] != null, x => x.disabled == Convert.ToInt32(map["status"]));

            exp.AndIF(map["startTime"] != null,x => x.createdTime <= Convert.ToDateTime(map["startTime"]) &&
                    x.createdTime <= Convert.ToDateTime(map["endTime"]));

            int total = 0;
       
            List<SysUserVo> list = db.Queryable<SysUser>().Where(exp.ToExpression()).Select(item =>
                new SysUserVo()
                {
                    id = item.id,
                    userName = item.userName,
                    nickName = item.nickName,
                    phoneNumber = item.phoneNumber,
                    disabled = item.disabled,
                    createdBy = item.createdBy,
                    createdTime = item.createdTime,
                    remark = item.remark
                }
            ).ToPageList((int)map["pageNumber"], (int)map["pageSize"], ref total);
            pager.total = total;
            foreach (var vo in list)
            {
                var query = db.Queryable<SysUserAndRole>()
                    .LeftJoin<SysRole>((sr, r) => sr.roleId == r.id)
                    .Where((sr, r) => sr.userId == vo.id).Select( (sr, r) => new SysRole()
                    {
                        roleKey = r.roleKey,
                        roleName = r.roleName,
                        disabled = r.disabled,
                        createdBy = r.createdBy,
                        createdTime = r.createdTime,
                        remark = r.remark,
                        id = r.id
                    });


                vo.roles.AddRange(query.ToList());
            }

            pager.rows = list;
            return pager;
        }

        public void Delete(long id)
        {
            db.Deleteable<SysUser>(x => x.id == id).ExecuteCommand();
        }


        public void Update(SysUserDto sysUserDto)
        {
            SysUser? sysUser = db.Queryable<SysUser>().Where(u => u.id == sysUserDto.id).Single();
            if (sysUser == null)
            {
                throw new BusinessException("修改用户不存在");
            }

            sysUser.nickName = sysUserDto.nickName;
            sysUser.phoneNumber = sysUserDto.phoneNumber;
            sysUser.remark = sysUserDto.remark;
            sysUser.disabled = sysUserDto.disabled;


            var result = db.Updateable<SysUser>()
               .SetColumns(x => x.nickName, sysUserDto.nickName)
               .SetColumns(x => x.phoneNumber, sysUserDto.phoneNumber)
               .SetColumns(x => x.remark, sysUserDto.remark)
               .SetColumns(x => x.disabled, sysUserDto.disabled).Where(x => x.id == sysUserDto.id).ExecuteCommand();
     
            // 更新用户角色      
            db.Deleteable<SysUserAndRole>(x => x.userId == sysUser.id).ExecuteCommand();
            serviceProvider.GetService<ISysRoleBll>()?.AddUserAndRole(sysUserDto.roleIds, sysUser.id);
        }

        public void UpdatePassword(long userId, string? password, string newPassword)
        {
            SysUser sysUser = db.Queryable<SysUser>().Where(u => u.id == userId).Single();
            if (sysUser == null)
            {
                throw new BusinessException("用户不存在");
            }
            if (!string.IsNullOrEmpty(password))
            {
                if (!sysUser.password.Equals(EncryptUtil.SHA256Encrypt(password)))
                {
                    throw new BusinessException("原密码错误");
                }
            }
            var result = db.Updateable<SysUser>()     
             .SetColumns(x => x.password, EncryptUtil.SHA256Encrypt(newPassword)).Where(x => x.id == userId).ExecuteCommand();
        }

        public UserInfo GetInfo(long id)
        {
              SysUser user = db.Queryable<SysUser>().Where(x => x.id == id).Single();
            if (null == user)
            {
                throw new BusinessException("用户不存在");
            }
            return new UserInfo(user.id, user.userName, user.nickName, user.phoneNumber, user.remark);
        }
    }
}
