using System.Diagnostics;
using System.Text;
using adminModule.Bll;
using domain.Pojo.sys;
using domain.Result;
using infrastructure.Attributes;
using infrastructure.Context;
using infrastructure.Utils;
using Newtonsoft.Json;
using Yitter.IdGenerator;

namespace webApi.Middlewares;

public class SysLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISysLogBll sysLogBll;
    private readonly ILogger<SysLogMiddleware> logger;

    public SysLogMiddleware(RequestDelegate next, ISysLogBll sysLog, ILogger<SysLogMiddleware> logger)
    {
        this._next = next;
        this.sysLogBll = sysLog;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string userId = HttpContextUtil.getUserId(context);
        string userName = HttpContextUtil.getUserName(context);
        RequestContextData requestContextData = new()
        {
            userId = string.IsNullOrEmpty(userId) ? null : long.Parse(userId),
            userName = userName
        };
        // 初始化 AsyncLocal
        using (RequestContext.SetContext(requestContextData))
        {
            long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var log = context.GetEndpoint()?.Metadata.GetMetadata<SysLogAttribute>();
            // 没有日志要求放行
            if (log == null)
            {
                await _next(context);
                return;
            }
            using Stream origin = context.Response.Body;
            using MemoryStream stream = new();
            context.Response.Body = stream;
            try
            {
                SysLog sysLog = new SysLog();
                sysLog.id = YitIdHelper.NextId();
                sysLog.operatorName = userName;
                sysLog.responseParam = string.Empty;
                sysLog.reason = string.Empty;
                sysLog.requestParam = await HttpContextUtil.getRequestParams(context);
                sysLog.operation = log.name;
                sysLog.path = context.Request.Path;
  
                await _next(context);
          
                stream.Position = 0;
                await stream.CopyToAsync(origin);

                if (HttpCode.SUCCESS_CODE != context.Response.StatusCode)
                    sysLog.executeStatus = false;
            
                sysLog.responseParam = Encoding.UTF8.GetString(stream.ToArray());
                ApiResult apiResult = JsonConvert.DeserializeObject<ApiResult>(sysLog.responseParam);
                if (HttpCode.SUCCESS_CODE != apiResult.code)
                    sysLog.executeStatus = false;
                long end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                sysLog.executeTime = end - start;
                sysLogBll.Save(sysLog);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"日志中间件异常，原因：{e.Message}");
            }
          
        }
    }
}