using adminModule;
using domain.Pojo.sys;
using domain.Result;
using infrastructure;
using infrastructure.Db;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Text;
using AspectCore.Extensions.DependencyInjection;
using dataPointsModule;
using domain.Pojo.ortherSystems;
using domain.Pojo.protocol;
using domain.Pojo.quartz;
using infrastructure.Utils;
using jobcoreModule;
using otherSystemModule;
using quartzModeule;
using webApi.Authorizations;
using webApi.Middlewares;


var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
//在Program 只需要配置这一句就行 aop生效
builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());


builder.AddInfrastructure();
builder.Services.AddAdmin();

#region 配置登录认证、授权

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true, //是否验证Issuer
        ValidIssuer = configuration["Jwt:Issuer"], //发行人Issuer
        ValidateAudience = true, //是否验证Audience
        ValidAudience = configuration["Jwt:Audience"], //订阅人Audience
        ValidateIssuerSigningKey = true, //是否验证SecurityKey
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"])), //SecurityKey
        ValidateLifetime = true, //是否验证失效时间
        ClockSkew = TimeSpan.FromSeconds(30), //过期时间容错值，解决服务器端时间不同步问题（秒）
        RequireExpirationTime = true,
    };
    // 当token验证通过后（执行完 JwtBearerEvents.TokenValidated 后），
    // 是否将token存储在 Microsoft.AspNetCore.Authentication.AuthenticationProperties 中
    // 默认 true
    options.SaveToken = true;
    options.Events = new JwtBearerEvents
    {
        //此处为权限验证失败后触发的事件
        OnChallenge = context =>
        {
            //此处代码为终止.Net Core默认的返回类型和数据结果，这个很重要哦，必须
            context.HandleResponse();
            //自定义自己想要返回的数据结果，我这里要返回的是Json对象，通过引用Newtonsoft.Json库进行转换
            var payload = JsonConvert.SerializeObject(new { code = HttpCode.UNAUTHORIZED_CODE, message = "很抱歉，您无权访问该；请登录！" });
            //自定义返回的数据类型
            context.Response.ContentType = "application/json";
            //自定义返回状态码，默认为401 我这里改成 200
            context.Response.StatusCode = StatusCodes.Status200OK;
            //输出Json数据结果
            context.Response.WriteAsync(payload);
            return Task.FromResult(0);
        }
    };
});
// 添加授权操作
builder.Services.AddAuthorization();
// 注册自定义授权处理器
builder.Services.AddSingleton<IAuthorizationHandler, PermissionsAuthorizationHandler>();
// 自定义授权失败结果返回
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomizeAuthorizationMiddlewareResultHandler>();

#endregion

builder.Services.AddDataPonitsModule();
builder.Services.AddQuartzModule();

builder.Services.AddOtherSystemModule();
builder.Services.AddJobCoreModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();

// 启用身份验证和授权中间件
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseMiddleware<SysLogMiddleware>();
app.UseMiddleware<OtherSystemsMiddleware>();
app.UseEndpoints(e => e.MapControllers());

app.UseStaticFilesInitPath();
app.UseServiceProvider();

// 初始化数据库表

/*
using var db = app.Services.GetService<DbClientFactory>().db;
var tables = db.DbMaintenance.GetTableInfoList(false);//true 走缓存 false不走缓存
if (tables == null || tables.Count == 0 )
{
 // 执行初始化sql
 db.CodeFirst.InitTables(typeof(SysUser), typeof(SysRole), typeof(SysRoleAndMenu), typeof(SysUserAndRole), typeof(SysMenu));
 db.CodeFirst.InitTables<SysLog>();  db.CodeFirst.InitTables<JobInfo>();
 db.CodeFirst.InitTables<JobLog>();    db.CodeFirst.InitTables<OtherSysLog>();
 db.CodeFirst.InitTables<OtherSysInfo>();
 db.CodeFirst.InitTables<S7DataPoint>();db.CodeFirst.InitTables<ProtocolLog>();
 db.CodeFirst.InitTables<SysSetting>();db.CodeFirst.InitTables<SysFileInfo>();
 db.CodeFirst.InitTables<InterfaceRequestConfig>();db.CodeFirst.InitTables<OpcUaDataPoint>();
 db.CodeFirst.InitTables<ModbusDataPoint>();
}
*/


// 数据点模块初始化
app.UseInitializesDataPointsModule();
// 定时任务模块初始化
app.UseInitializesQuartzModule();


app.Run();


public partial class Program { }