
using domain.Dto;
using domain.Enums;
using domain.Pojo.jobCore;
using domain.Records;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Exceptions;
using infrastructure.Utils;
using jobcoreModule.Core;
using jobcoreModule.TaskTemplate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace jobcoreModule.Bll.Impl;

/// <summary>
/// 任务管理
/// </summary>
[Service]
public class TaskInfoBll : ITaskInfoBll
{
    private readonly ILogger<TaskInfoBll> _logger;
    private readonly ISqlSugarClient db;
    private readonly JobCoreContext jobCoreContext;
    private readonly ITaskTemplate taskTemplate;

    public TaskInfoBll(ILogger<TaskInfoBll> logger, DbClientFactory dbClientFactory, JobCoreContext jobCoreContext, 
        ITaskTemplate taskTemplate)
    {
        this._logger = logger;
        this.db = dbClientFactory.db;
        this.jobCoreContext = jobCoreContext;
    }
    
    public void Initialized()
    {
        List<TaskInfo> list = db.Queryable<TaskInfo>().Where(x => x.executeStatus == TaskState.Started 
                                            || x.executeStatus == TaskState.Pause).ToList();
        list.ForEach(item =>
        {
            item.executeStatus = TaskState.Fail;
            item.interrupt = 1;
            Update(item);
        });
    }
    
    public void Save(TaskInfoDto dto)
    {
        TaskInfo taskInfo = new();
        taskInfo.id = CreatedTaskId();
        taskInfo.taskName = dto.taskName;
        taskInfo.taskType = dto.taskType;
        taskInfo.inputParam = dto.inputParam;
        taskInfo.outputParam = dto.outputParam;
        db.Insertable<TaskInfo>(taskInfo).ExecuteCommand();

    }

    public Pager<TaskInfo> GetList(TaskInfoQuery query)
    {

        Pager<TaskInfo> pager = new(query.pageNum, query.pageSize);
        var exp = Expressionable.Create<TaskInfo>();
        exp.AndIF(!string.IsNullOrEmpty(query.name), x => x.taskName.Contains(query.name));
        exp.AndIF(query.interrupt != null, x => x.interrupt == query.interrupt);
        exp.AndIF(query.type != null, x => x.taskType == query.type);
        exp.AndIF(query.executeStatus != null, x => (int)x.executeStatus == query.executeStatus);
        exp.AndIF(query.startTime != null, x => x.createTime >= query.startTime);
        exp.AndIF(query.endTime != null, x => x.createTime <= query.endTime);

        pager.rows = db.Queryable<TaskInfo>().Where(exp.ToExpression()).OrderByDescending(x => x.createTime)
            .Skip(pager.getSkip())
            .Take(pager.pageSize).ToList();
        pager.total = db.Queryable<TaskInfo>().Where(exp.ToExpression()).Count();
        return pager;
    }

    public List<TaskInfo> GetListByTaskState(TaskState executeStatus)
    {
        return db.Queryable<TaskInfo>().Where(x => x.taskType == (int)executeStatus).ToList();
    }

    public void Del(List<string> ids)
    {
        if (ids.Count < 1)
        {
            return;
        }
        db.Deleteable<TaskInfo>().In(ids).ExecuteCommand();
    }

    public void Update(TaskInfo taskInfo)
    {
        db.Updateable(taskInfo).ExecuteCommand();
    }

    public void Scheduling(string taskId, TaskState state)
    {
        TaskInfo? taskInfo = db.Queryable<TaskInfo>().Where(x => x.id.Equals(taskId)).First();
        if (taskInfo != null)
        {
            throw new BusinessException("任务不存在，操作失败");
        }

        if (state == TaskState.Continue)
        {
            taskInfo.executeStatus = TaskState.Started;
            int i = db.Updateable(taskInfo).ExecuteCommand();
            if (i > 0)
            {
                jobCoreContext.SetTask(taskInfo.id);
            }
        }
        if (state == TaskState.Pause)
        {
            taskInfo.executeStatus = TaskState.Started;
            int i = db.Updateable(taskInfo).ExecuteCommand();
            if (i > 0)
            {
                jobCoreContext.PuseTask(taskInfo.id);
            }
        }
        if (state == TaskState.Terminate)
        {
            taskInfo.executeStatus = TaskState.Started;
            int i = db.Updateable(taskInfo).ExecuteCommand();
            if (i > 0)
            {
                jobCoreContext.CancelTask(taskInfo.id);
            }
        }
        
    }

    public void StartNewJob(TaskInfo taskInfo)
    {
        var c = jobCoreContext.GetCancellationToken(taskInfo.id);
        Task.Factory.StartNew((taskId) =>
        {
            Thread.CurrentThread.Name = Convert.ToString(taskId);
            jobCoreContext.WaitOne(taskId.ToString());
            try
            {
                _logger.LogInformation($"{Thread.CurrentThread.Name}-线程开始工作》》》》》》》");
                jobCoreContext.Monitor(Thread.CurrentThread.Name);
                Running(taskId.ToString());
                jobCoreContext.Monitor(Thread.CurrentThread.Name);
                Thread.Sleep(500);
            }
            catch (ThreadInterruptedException e)
            {
                _logger.LogError($"{Thread.CurrentThread.Name}-线程终止" + e.Message);
                jobCoreContext.RemoveThread(Thread.CurrentThread.Name);
            }
            catch (Exception exception)
            {
                _logger.LogError($"{Thread.CurrentThread.Name}-线程异常终止：{ exception.Message}");
                jobCoreContext.RemoveThread(Thread.CurrentThread.Name);
                taskInfo.executeStatus = TaskState.Fail;
                Update(taskInfo);
            }

        }, taskInfo.id, c);
    }



    private string CreatedTaskId()
    {
        return DateTime.Now.ToString("yyyyMMddHHmmss");
    }

    /// <summary>
    /// 根据任务类型执行任务配置模板
    /// </summary>
    /// <param name="taskId"></param>
    private void Running(string taskId)
    {
        TaskInfo taskInfo = db.Queryable<TaskInfo>().First(x => x.id.Equals(taskId));

        switch (taskInfo.taskType)
        {
            case 1:
                break;
            
            default:
                taskTemplate.test(taskInfo.inputParam);
                break;
        }
        

    }
    
}