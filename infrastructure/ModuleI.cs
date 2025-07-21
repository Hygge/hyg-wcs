using AspectCore.Extensions.DependencyInjection;
using domain.Pojo.config;
using infrastructure.Configs;
using infrastructure.Extensions;
using infrastructure.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Yitter.IdGenerator;


namespace infrastructure
{
    public static class ModuleI
    {
        public static void AddInfrastructure(this WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            
            builder.Services.Configure<BaseSetting>(configuration.GetSection(nameof(BaseSetting)));
            
            builder.Services.AddControllers(opt =>
             {
                 // 统一设置路由前缀
                 opt.UseCentralRoutePrefix(new Microsoft.AspNetCore.Mvc.RouteAttribute(builder.Configuration["context-path"]));
             });
            // 注册筛选器
            builder.Services.Configure<MvcOptions>(opt =>
            {
                opt.Filters.Add<GlobalExceptionFilter>();

            });
            //统一返回时间格式,配置返回的时间类型数据格式
            builder.Services.AddMvc().AddJsonOptions((options) => {
                options.JsonSerializerOptions.Converters.Add(new DatetimeJsonConverter());
            });
            // 日志
            builder.Services.AddLogging(log =>
            {
                log.AddConsole();
                log.AddDebug();
                log.AddLog4Net();
            });
            
            // Add services to the container.
            builder.Services.AddServices();
            builder.Services.AddComponents();

            // 雪花算法配置
            var options = new IdGeneratorOptions(6);
            YitIdHelper.SetIdGenerator(options);

            // 启动访问上下文 同理也可以使用自定义接口token获取对应上下文参数
            builder.Services.AddHttpContextAccessor();



            #region Swagger配置
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(s =>
            {
                //添加安全定义
                s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "请输入token,格式为 Bearer xxxxxxxx（注意中间必须有空格）",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                //添加安全要求
                s.AddSecurityRequirement(new OpenApiSecurityRequirement {
                        {
                            new OpenApiSecurityScheme{
                                Reference =new OpenApiReference{
                                    Type = ReferenceType.SecurityScheme,
                                    Id ="Bearer"
                                }
                            },new string[]{ }
                        }
                    });
                });

            #endregion
        }
        
        
        public static void UseStaticFilesInitPath(this IApplicationBuilder app)
        {
            string path = Path.Combine(app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath, 
                app.ApplicationServices.GetRequiredService<IOptions<BaseSetting>>().Value.filePath);
            if(!Directory.Exists(path))
                Directory.CreateDirectory(path);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(path),
                RequestPath = string.Empty,
            });
            // todo 如果文件需要授权访问都话，可使用自定义中间件校验请求前缀文件目录
        }
        
    }
}
