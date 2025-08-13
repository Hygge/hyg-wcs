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
        ValidateIssuer = true, 
        ValidIssuer = configuration["Jwt:Issuer"], 
        ValidateAudience = true,
        ValidAudience = configuration["Jwt:Audience"], 
        ValidateIssuerSigningKey = true, 
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"])),
        ValidateLifetime = true, 
        ClockSkew = TimeSpan.FromSeconds(30),
        RequireExpirationTime = true,
    };
    options.SaveToken = true;
    options.Events = new CustomJwtBearerEvents();
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

app.UseMiddleware<CusResultMiddleware>();

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