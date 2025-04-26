using domain.Enums;
using domain.Pojo.quartz;
using infrastructure.Attributes;
using infrastructure.Db;
using infrastructure.Exceptions;
using infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl.Matchers;
using SqlSugar;
using Yitter.IdGenerator;

namespace quartzModeule.Bll.Impl;

[Service]
public class JobInfoBll : IJobInfoBll
{

    private readonly IScheduler scheduler;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISqlSugarClient db;
    
    public JobInfoBll(ISchedulerFactory schedulerFactory, DbClientFactory _dbClientFactory, IServiceProvider _serviceProvider)
    {
        this._serviceProvider = _serviceProvider;
        this.scheduler = schedulerFactory.GetScheduler().Result;
        this.db = _dbClientFactory.db;
    }
    
    
    public async Task Save(JobInfo jobInfo)
    {
        jobInfo.id = YitIdHelper.NextId();
         JobInfo jobInfo1 = db.Queryable<JobInfo>().Where(x => x.jobName.Equals(jobInfo.jobName) 
                                            || x.jobKey.Equals(jobInfo.jobKey)).Single();
         if (null != jobInfo1)
         {
             throw new BusinessException(424, "计划名称或计划key已存在");
         }
        int ret = db.Insertable<JobInfo>(jobInfo).ExecuteCommand();
        if (ret == 1 && jobInfo.status == (int) JobStatuEnum.RUNNING)
        {
           await scheduler.ScheduleJob(JobDetailBuild(jobInfo), TriggerBuild(jobInfo));
        }
    }

    public async Task Update(JobInfo jobInfo)
    {
        JobInfo jobInfo1 = db.Queryable<JobInfo>().Where(x => x.id == jobInfo.id).Single();
        if (null == jobInfo1)
        {
            throw new BusinessException(424, "计划不存在修改保存失败");
        }

        jobInfo1.jobName = jobInfo.jobName;
        jobInfo1.category = jobInfo.category;
        jobInfo1.cronExpression = jobInfo.cronExpression;
        jobInfo1.seconds = jobInfo.seconds;
        jobInfo1.repeat = jobInfo.repeat;
        jobInfo1.status = jobInfo.status;
        jobInfo1.misfirePolicy = jobInfo.misfirePolicy;
        jobInfo1.concurrent = jobInfo.concurrent;
        
        int ret = db.Updateable(jobInfo1).ExecuteCommand();
        if (ret != 1)
        {
            return;
        }
        // 修改完成后删除原计划，判断计划执行状态，1正常状态重新生成计划执行 2暂停状态无操作
        await DeleteJob(jobInfo1);
        if ((int)JobStatuEnum.RUNNING == jobInfo1.status)
        {
           await scheduler.ScheduleJob(JobDetailBuild(jobInfo1), TriggerBuild(jobInfo1));
        }

    }

    public async Task RuningJob(long id)
    {
        JobInfo jobInfo = db.Queryable<JobInfo>().Single(x => x.id == id);
        if (jobInfo == null)
        {
            throw new BusinessException(424, "计划不存在继续失败");
        }
        jobInfo.status = (int)JobStatuEnum.RUNNING;
        int ret = db.Updateable(jobInfo).ExecuteCommand();
        if (ret == 1)
        {
            await ResumeJob(jobInfo);
        }
    }

    public async Task Delete(List<long> ids)
    {
        foreach (var id in ids)
        {
            JobInfo jobInfo = db.Queryable<JobInfo>().Single(x => x.id == id);
            if (jobInfo == null)
            {
                continue;
            }
            int ret = db.Deleteable(jobInfo).ExecuteCommand();
            if (ret == 1)
            {
                await DeleteJob(jobInfo);  
            } 
        }
    }

    public async Task PausedJob(long id)
    {
        var jobInfo = db.Queryable<JobInfo>().Single( x => x.id == id);
        if (null == jobInfo)
        {
            throw new BusinessException(424, "计划不存在暂停失败");
        }
        jobInfo.status = (int)JobStatuEnum.PAUSED;
        int ret = db.Updateable(jobInfo).ExecuteCommand();
        if (ret == 1)
        {
          await PauseJob(jobInfo);
        }
    }

    public async Task<Pager<JobInfo>> GetList(string? jobName, string? category, int? status, int pageNum = 1, int pageSize = 10)
    {
        Pager<JobInfo> pager = new Pager<JobInfo>(pageNum, pageSize);
        var expr = Expressionable.Create<JobInfo>();
        expr.AndIF(!string.IsNullOrEmpty(jobName), j => j.jobName.Contains(jobName));
        expr.AndIF(!string.IsNullOrEmpty(category), j => j.category.Contains(category));
        expr.AndIF(null != status , j => j.status == status);
        
        pager.rows = await db.Queryable<JobInfo>().Where(expr.ToExpression()).Skip(pager.getSkip()).Take(pager.pageSize).ToListAsync();
        pager.total = await db.Queryable<JobInfo>().Where(expr.ToExpression()).CountAsync();

        return pager;
    }

    
    /// <summary>
    /// 初始化加载正常运行计划
    /// </summary>
    public void InitJobs()
    {
        List<JobInfo> list = db.Queryable<JobInfo>().ToList();
        list.ForEach(item =>
        {
            if ((int)JobStatuEnum.RUNNING == item.status)
            {
                scheduler.ScheduleJob(JobDetailBuild(item), TriggerBuild(item));
            }
        });

    }


    /// <summary>
    /// 创建执行计划
    /// </summary>
    /// <param name="jobInfo"></param>
    /// <returns></returns>
    private IJobDetail JobDetailBuild(JobInfo jobInfo)
    {
       // _serviceProvider.GetService(ReflectionUtil.GetType(jobInfo.assemblyName, jobInfo.typeName))
        JobBuilder builder = JobBuilder.Create(ReflectionUtil.GetType(jobInfo.assemblyName, jobInfo.typeName))
            .WithIdentity(jobInfo.jobKey, jobInfo.jobGroup);
        if (!jobInfo.concurrent)
        {
           builder = builder.DisallowConcurrentExecution();
        }
        builder = builder.StoreDurably().RequestRecovery();
        return builder.Build();
    }

    /// <summary>
    /// 创建触发器
    /// </summary>
    /// <param name="jobInfo"></param>
    /// <returns></returns>
    private ITrigger TriggerBuild(JobInfo jobInfo)
    {
        TriggerBuilder builder = TriggerBuilder.Create()
            .WithIdentity(jobInfo.jobKey + "trigger", jobInfo.jobGroup);
        // 存在cron表达式使用该执行策略
        if (!string.IsNullOrEmpty(jobInfo.cronExpression))
        {
            builder = builder .WithCronSchedule(jobInfo.cronExpression).ForJob(jobInfo.jobKey, jobInfo.jobGroup);
        }
        else
        {
            builder = builder.WithSimpleSchedule(x =>
            {
                x.WithIntervalInSeconds(jobInfo.seconds);
                if (-1 == jobInfo.repeat)
                {
                    x.RepeatForever();
                }
                else
                {
                    x.WithRepeatCount(jobInfo.repeat);
                }
            });
        }
        if ((int)JobMisfirePolicyEnum.NOW == jobInfo.misfirePolicy)
        {
            builder = builder.StartNow();
        }
        return builder.Build();
    }
    
    /// <summary>
    /// 暂停调度中计划
    /// </summary>
    /// <param name="jobInfo"></param>
    /// <exception cref="SchedulerException"></exception>
    private async Task PauseJob(JobInfo jobInfo)
    {
        //查询同一个分组的JobKey列表
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(jobInfo.jobGroup));
        if (jobKeys != null && jobKeys.Count > 0)
        {
            //获得指定名称jobName的的jobKey
            var jobKey = jobKeys.FirstOrDefault(p => p.Name.Equals(jobInfo.jobKey));
            if (jobKey != null)
            {
                //暂停job的调度
                await scheduler.PauseJob(jobKey);
               // scheduler.ResumeJob(new JobKey(jobName));
            }
            else
            {
                throw new SchedulerException("Can' find this Job.");
            }
        }
        else
        {
            throw new SchedulerException("Can't fin this Job.");
        }
    }

    /// <summary>
    /// 删除调度中的计划
    /// </summary>
    /// <param name="jobInfo"></param>
    private async Task DeleteJob(JobInfo jobInfo)
    {
        // 存在就继续，不存在就创建
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(jobInfo.jobGroup));
        if (jobKeys != null && jobKeys.Count > 0)
        {
            //获得指定名称jobName的的jobKey
            var jobKey = jobKeys.FirstOrDefault(p => p.Name.Equals(jobInfo.jobKey));
            if (jobKey != null)
            {
                await scheduler.DeleteJob(jobKey);
            }
            else
            {
                throw new SchedulerException("Can' find this Job.");
            }
        }
        else
        {
            return;
        }
    }

    /// <summary>
    /// 恢复调度中的计划
    /// </summary>
    /// <param name="jobInfo"></param>
    private async Task ResumeJob(JobInfo jobInfo)
    {
        // 存在就继续，不存在就创建
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(jobInfo.jobGroup));
        if (jobKeys != null && jobKeys.Count > 0)
        {
            //获得指定名称jobName的的jobKey
            var jobKey = jobKeys.FirstOrDefault(p => p.Name.Equals(jobInfo.jobKey));
            if (jobKey != null)
            {
                await scheduler.ResumeJob(jobKey);
            }
            else
            {
                await scheduler.ScheduleJob(JobDetailBuild(jobInfo), TriggerBuild(jobInfo));
            }
        }
        else
        {
            await scheduler.ScheduleJob(JobDetailBuild(jobInfo), TriggerBuild(jobInfo));
        }
    }
    
    
}