
using AspectCore.DynamicProxy;
using dataPointsModule.Bll;
using domain.Dto;
using domain.Enums;
using domain.Pojo.protocol;
using infrastructure.Exceptions;
using infrastructure.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Yitter.IdGenerator;

namespace dataPointsModule.Attributes;

/// <summary>
/// 协议读写日志记录
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ProtocolLogAttribute : AbstractInterceptorAttribute
{
    private readonly IProtocolLogBll _protocolLogBll = ServiceUtil.ServiceProvider.GetRequiredService<IProtocolLogBll>();
    private string opt;
    private string protocolEnum;
    public ProtocolLogAttribute(OperateEnum opt, ProtocolEnum protocolEnum)
    {
        this.opt = Enum.GetName(opt);
        this.protocolEnum = Enum.GetName(protocolEnum);
    }
    
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        ProtocolLog protocolLog = new();
        long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        try
        {
            Dictionary<string, object> map =JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(context.Parameters[0]));
            protocolLog.id = YitIdHelper.NextId();
            string name = map.TryGetValue("name", out object obj) ? Convert.ToString(obj) : string.Empty;
            //string category = map.TryGetValue("category", out object obj1) ? Convert.ToString(obj1) : string.Empty;
            
            protocolLog.name = name;
            protocolLog.category = protocolEnum;
            
            protocolLog.oper = opt;
            
            await next(context);
            if (opt.Equals(Enum.GetName(OperateEnum.Read)))
            {
                protocolLog.value = Convert.ToString((context.ReturnValue as DataPointDto).value) ;
            }
            else
            {
                protocolLog.value = context.Parameters[1].ToString();
            }
            
            protocolLog.reson = Convert.ToString((context.ReturnValue as DataPointDto).msg) ;
            protocolLog.status = (context.ReturnValue as DataPointDto).state ;
        }
        catch (Exception ex)
        {
            protocolLog.status = false;
            protocolLog.reson = ex.Message;
            throw new DeviceException(ex.Message);
        }
        protocolLog.endTime = DateTime.Now;
        protocolLog.time = DateTimeOffset.Now.ToUnixTimeMilliseconds() - start;
        _protocolLogBll.Save(protocolLog);
    }
    
}