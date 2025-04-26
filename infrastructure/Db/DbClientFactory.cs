using System.Text;
using domain.Enums;
using infrastructure.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;



namespace infrastructure.Db;

[Component(ServiceLifetime.Scoped)]
public class DbClientFactory
{
    public readonly ISqlSugarClient db;
    private readonly ILogger<DbClientFactory> _logger;
    public DbClientFactory(IConfiguration configuration, ILogger<DbClientFactory> _logger) 
    {
        this._logger = _logger;
        this.db = GetSqlSugarClient(configuration.GetConnectionString("pgsql"));
    }

    public ISqlSugarClient GetSqlSugarClient(string connectionStringSettings)
    {
        var db = new SqlSugarClient(new ConnectionConfig()
        {
            DbType = DbType.PostgreSQL,
            ConnectionString = connectionStringSettings,
            IsAutoCloseConnection = true,
            ConfigureExternalServices = new ConfigureExternalServices()
            {
                EntityService = (x, p) => 
                {
                    p.DbColumnName = UtilMethods.ToUnderLine(p.DbColumnName);//ToUnderLine驼峰转下划线方法
                },
                EntityNameService = (x, p) =>
                {
                    p.DbTableName = UtilMethods.ToUnderLine(p.DbTableName);//ToUnderLine驼峰转下划线方法
                }
            }
        });
        db.Aop.OnLogExecuting = (sql, pars) =>
        {
            StringBuilder sb = new();
            foreach (var parameter in pars)
            {
                sb.Append($"{parameter.ParameterName} = {parameter.Value}");
                sb.Append("\t");
            }
            _logger.LogInformation($"正在执行sql：{sql} \t，参数：{sb.ToString()}");

        };
        db.Aop.OnError = (exp) =>//SQL报错
        {
            _logger.LogError($"{exp.Message}");
        };  
        return db;
    }

    /// <summary>
    /// 构建分页查询SQL
    /// </summary>
    /// <param name="sql">查询SQL</param>
    /// <param name="pageNum">当前页数</param>
    /// <param name="pageSize">页面记录数量</param>
    /// <param name="db"></param>
    /// <param name="orderByField">排序字段（PostgreSQL、MySql忽略）</param>
    /// <param name="orderByEnum">排序规则（PostgreSQL、MySql忽略）</param>
    /// <returns></returns>
    public string PagerSqlBuilder(string sql, int pageNum, int pageSize,
        ISqlSugarClient db, string? orderByField, OrderByEnum? orderByEnum)
    {

        if (db.CurrentConnectionConfig.DbType == DbType.PostgreSQL)
            return pagerPostgreSqlBuilder(sql, pageNum, pageSize);
        if (db.CurrentConnectionConfig.DbType == DbType.SqlServer)
            return pagerSqlServerBuilder(sql, pageNum, pageSize, orderByField, orderByEnum);
        if (db.CurrentConnectionConfig.DbType == DbType.MySql)
            return pagerMySqlBuilder(sql, pageNum, pageSize);
        return string.Empty;
    }

    private string pagerSqlServerBuilder(string sql, int pageNum, int pageSize, string orderByField,
        OrderByEnum? orderByEnum)
    {
        StringBuilder sb = new();
        sb.Append("select t.* from ( ");
        sb.Append(sql);
        sb.Append(" ) as t");

        if (!string.IsNullOrEmpty(orderByField))
        {
            sb.Append($" order by t.{orderByField} ");
            if (orderByEnum is not null)
                sb.Append(nameof(orderByEnum));
            else
                sb.Append(nameof(OrderByEnum.ASC));
        }
        sb.Append($" offset  {(pageNum - 1) * pageSize}  row fetch next {pageSize}  row only ;");
        return sb.ToString();
    }
    
    private string pagerPostgreSqlBuilder(string sql, int pageNum, int pageSize)
    {
        StringBuilder sb = new();
        sb.Append(" WITH t AS ( ");
        sb.Append(sql);
        sb.Append($") SELECT * FROM t LIMIT {pageSize} OFFSET {(pageNum - 1) * pageSize} ;");
        return sb.ToString();
    }
    
    private string pagerMySqlBuilder(string sql, int pageNum, int pageSize)
    {
        StringBuilder sb = new();
        sb.Append(sql);
        sb.Append($" LIMIT {(pageNum - 1) * pageSize},{pageSize} ;");
        return sb.ToString();
    }

    /// <summary>
    /// 构建查询数据统计SQL
    /// </summary>
    /// <param name="sql">查询SQL</param>
    /// <returns></returns>
    public string pagerCountBuilder(string sql)
    {
        StringBuilder sb = new();
        sb.Append("select count(t.*) from ( ");
        sb.Append($"{sql} ) as t");
        return sb.ToString();
    }
    
}

