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

    public TransactionScopeOption option;
    public TransactionScopeAttribute(TransactionScopeOption option){
        this.option = option;
    }
    public TransactionScopeAttribute(){
        
    }
           
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        if(option == null)
            option = TransactionScopeOption.Suppress;
        using var scope = new TransactionScope(option);
         try{

            await next(context);
            scope.Complete();

         }catch(Exception ex)
         {
            ServiceUtil.GetRequiredService<ILogger<TransactionScopeAttribute>>().LogError(ex, $"当前事务异常，原因：{ex.Message}");
            throw new BusinessException(ex.Message);
         }
    }

}
