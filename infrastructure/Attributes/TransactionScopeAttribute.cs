using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AspectCore.DynamicProxy;
using infrastructure.Exceptions;
using infrastructure.Utils;
using Microsoft.Extensions.Logging;

namespace infrastructure.Attributes;

/// <summary>
/// 事务
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TransactionScopeAttribute : AbstractInterceptorAttribute
{

    private readonly TransactionScopeOption option;
    private readonly TransactionScopeAsyncFlowOption optionAsync;
    public TransactionScopeAttribute(TransactionScopeOption option){
        this.option = option;
    }
    public TransactionScopeAttribute(TransactionScopeAsyncFlowOption optionAsync){
        this.optionAsync = optionAsync;
    }
    
    public TransactionScopeAttribute()
    {
        this.optionAsync = TransactionScopeAsyncFlowOption.Enabled;
        this.option = TransactionScopeOption.Suppress;
    }
           
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        if (context.IsAsync())
        {
            using var scope = new TransactionScope(optionAsync);
            try
            {
                await next(context);
                scope.Complete();

            }catch(Exception ex)
            {
                ServiceUtil.GetRequiredService<ILogger<TransactionScopeAttribute>>().LogError(ex, $"当前事务异常，原因：{ex.Message}");
                throw new BusinessException(ex.Message);
            }
        }
        else
        {
            using var scope = new TransactionScope(option);
            try
            {
                await next(context);
                scope.Complete();

            }catch(Exception ex)
            {
                ServiceUtil.GetRequiredService<ILogger<TransactionScopeAttribute>>().LogError(ex, $"当前事务异常，原因：{ex.Message}");
                throw new BusinessException(ex.Message);
            }
        }

    }
    

}
