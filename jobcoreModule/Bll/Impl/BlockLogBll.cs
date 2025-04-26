using domain.Pojo.jobCore;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Yitter.IdGenerator;

namespace jobcoreModule.Bll.Impl;


[Service]
public class BlockLogBll : IBlockLogBll
{
    
    private readonly ILogger<IBlockLogBll> _logger;
    private readonly ISqlSugarClient db;

    public BlockLogBll(ILogger<IBlockLogBll> _logger, DbClientFactory _dbClientFactory)
    {
        this._logger = _logger;
        this.db = _dbClientFactory.db;
    }
    
    public void Save(BlockLog log)
    {
        db.Insertable<BlockLog>(log).ExecuteCommand();
    }

    public void Update(BlockLog log)
    {
        db.Updateable(log).ExecuteCommand();
    }

    public BlockLog GetByTaskIdAndBId(string taskId, string bId)
    {
        return db.Queryable<BlockLog>().First(x => x.taskId.Equals(taskId) && x.bId.Equals(bId));
    }
}