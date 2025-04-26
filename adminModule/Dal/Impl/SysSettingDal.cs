using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using domain.Pojo.sys;
using infrastructure.Attributes;
using infrastructure.Db;
using SqlSugar;

namespace adminModule.Dal.Impl
{
    [Service]
    public class SysSettingDal : ISysSettingDal
    {

        private readonly ISqlSugarClient db;
        public SysSettingDal(DbClientFactory dbClientFactory)
        {
            db = dbClientFactory.db;
        }


        public void Insert(SysSetting sysSetting)
        {
            db.Insertable<SysSetting>(sysSetting).ExecuteCommand();
        }

        public void Update(SysSetting sysSetting)
        {
            db.Updateable<SysSetting>(sysSetting).ExecuteCommand();
        }

        public SysSetting SelectByName(string keyName)
        {
            return db.Queryable<SysSetting>().Single( s => s.keyName.Equals(keyName));
        }

        public SysSetting SelectByKey(string keyName)
        {
            return db.Queryable<SysSetting>().Single( s => s.keyName.Equals(keyName));
        }
        
    }

}