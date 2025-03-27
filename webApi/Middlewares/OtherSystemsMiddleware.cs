using System.Net;
using System.Text;
using domain.Pojo.ortherSystems;
using domain.Result;
using infrastructure.Attributes;
using infrastructure.Exceptions;
using infrastructure.Utils;
using Newtonsoft.Json;
using otherSystemModule.Bll;
using Yitter.IdGenerator;

namespace webApi.Middlewares;

public class OtherSystemsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOtherSysLogBll otherSysLog;
    private readonly IOtherSysInfoBll otherSysInfo;

    public OtherSystemsMiddleware(RequestDelegate next, IOtherSysLogBll otherSysLog, IOtherSysInfoBll otherSysInfo)
    {
        this._next = next;
        this.otherSysLog = otherSysLog;
        this.otherSysInfo = otherSysInfo;
    }

    public async Task InvokeAsync(HttpContext context)
    {
    
        long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var log = context.GetEndpoint()?.Metadata.GetMetadata<OtherSystemsInterfaceAttribute>();
        // 没有第三方对接要求放行
        if (log == null)
        {
            await _next(context);
            return;
        }
        using Stream origin = context.Response.Body;
        using MemoryStream stream = new();
        context.Response.Body = stream;
        OtherSysLog sysLog = new OtherSysLog();
        try
        {
            sysLog.id = YitIdHelper.NextId();
            sysLog.operatorName = HttpContextUtil.getUserName(context);
            sysLog.responseParam = string.Empty;
            sysLog.reason = string.Empty;
            sysLog.requestParam = await HttpContextUtil.getRequestParams(context);
            sysLog.operation = log.name;
            sysLog.path = context.Request.Path;
            sysLog.sysTargetName = "WCS";
            sysLog.ip = HttpContextUtil.getRequestIp(context);
            
            // 校验该系统是否有权限
            otherSysInfo.IsPermissions(context.Request.Headers["appKey"], sysLog);
  
            await _next(context);
            
          
            stream.Position = 0;
            await stream.CopyToAsync(origin);

            if (HttpCode.SUCCESS_CODE != context.Response.StatusCode)
                sysLog.executeStatus = false;
            
            sysLog.responseParam = Encoding.UTF8.GetString(stream.ToArray());
            ApiResult apiResult = JsonConvert.DeserializeObject<ApiResult>(sysLog.responseParam);
            if (HttpCode.SUCCESS_CODE != apiResult.code)
                sysLog.executeStatus = false;
            
        }
        catch (Exception e)
        {
            ExceptionHandle(e, context, stream, sysLog);
            //重置流游标
            stream.Position = 0;
            await stream.CopyToAsync(origin);
        }
        long end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        sysLog.executeTime = end - start;
        otherSysLog.Save(sysLog);
    }


    /// <summary>
    /// 业务异常和其他未知异常处理
    /// </summary>
    /// <param name="e"></param>
    /// <param name="context"></param>
    /// <param name="stream"></param>
    /// <param name="sysLog"></param>
    private async Task ExceptionHandle(Exception e, HttpContext context, MemoryStream stream, OtherSysLog sysLog)
    {
        BusinessException exception = e as BusinessException;
        sysLog.executeStatus = false;
     
        if ( exception != null)
        {
            sysLog.reason = exception.message;
            context.Response.ContentType = "application/json";
            ApiResult result = ApiResult.failed(exception.code, exception.message);
            sysLog.responseParam = JsonConvert.SerializeObject(result);
            stream.Write(Encoding.UTF8.GetBytes(sysLog.responseParam));
        }
        else
        {
            sysLog.reason = e.Message;
            context.Response.ContentType = "application/json";
            ApiResult result = ApiResult.failed(HttpCode.FAILED_UNKNOWN_CODE, e.Message);
            sysLog.responseParam = JsonConvert.SerializeObject(result);
            stream.Write(Encoding.UTF8.GetBytes(sysLog.responseParam));
        }
        
    }
    
    
}