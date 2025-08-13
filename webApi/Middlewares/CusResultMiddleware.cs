using domain.Result;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;

namespace webApi.Middlewares;

public class CusResultMiddleware
{
    private readonly RequestDelegate _next;


    public CusResultMiddleware(RequestDelegate next)
    {
        this._next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            var authenticateResult = await context.AuthenticateAsync();
            string errorMsg = authenticateResult.Failure is not null
                ? authenticateResult.Failure.Message
                : "很抱歉，您无权访问该；请登录！";
            var error = authenticateResult.Properties?.GetString("error");
            var errorDescription = authenticateResult.Properties?.GetString("error_description");
            Console.WriteLine(errorDescription );
            Console.WriteLine(error );
            //自定义自己想要返回的数据结果，我这里要返回的是Json对象，通过引用Newtonsoft.Json库进行转换
            var payload = JsonConvert.SerializeObject(new { code = HttpCode.UNAUTHORIZED_CODE, message = errorMsg });
            //自定义返回的数据类型
            context.Response.ContentType = "application/json";
            //自定义返回状态码，默认为401 我这里改成 200
            context.Response.StatusCode = StatusCodes.Status200OK;
            //输出Json数据结果
            context.Response.WriteAsync(payload);
            
        }
        Console.WriteLine(context.Response.ContentType);
    
    }

}