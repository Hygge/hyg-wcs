using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using domain.Result;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;

namespace webApi.Authorizations;

public class CustomJwtBearerEvents  : JwtBearerEvents
{
    
    public CustomJwtBearerEvents()
    {
    }

    /// <summary>
    /// token校验成功后，需要其他业务校验可在此实现，（需要过滤匿名权限接口）
    /// </summary>
    /// <param name="context"></param>
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        
        if (false)
        {
            var accessToken = context.SecurityToken as JwtSecurityToken;
            context.Fail("User account is disabled"); // 失败使用
            return;
        }
        // 所有校验通过
        await base.TokenValidated(context);
    }


}