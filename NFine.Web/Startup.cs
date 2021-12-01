using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NFine.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace NFine.Web
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            // System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)//增加环境配置文件，新建项目默认有
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IMvcBuilder mvcBuilder = services.AddControllersWithViews();

            services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
            });

            //添加mvc自定义寻址Razor页面地址
            mvcBuilder.AddRazorOptions(options =>
            {
                options.ViewLocationExpanders.Add(new NamespaceViewLocationExpander());
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
             {
                 options.LoginPath = "/Login/Index";
                 options.Cookie.Name = "AuthCookie";
             });

            services.AddDbContextPool<NFineDbContext>(optionsAction =>
            {
                optionsAction.UseSqlServer(Configuration.GetConnectionString("NFineDbContext"));
            });

            services.AddScoped<IRepositoryBase, RepositoryBase>();

            #region 注入repositorybase类
            Assembly asm = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("NFine.Repository"));
            var typesToRegister = asm.GetTypes()
           .Where(type => !String.IsNullOrEmpty(type.Namespace) && type.IsPublic).Where(type => type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(RepositoryBase<>));

            foreach (var type in typesToRegister)
            {
                if (type.IsClass)
                {
                    services.AddScoped(type.GetInterface("I" + type.Name), type);
                }
            }
            #endregion

            #region 注入app类
            var nfineApplication = Microsoft.Extensions.DependencyModel.DependencyContext.Default.CompileLibraries.FirstOrDefault(_ => _.Name.Equals("NFine.Application"));
            var application = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(nfineApplication.Name));
            var apps = application.GetTypes().Where(_ => _.IsClass && _.IsPublic).Where(type => !String.IsNullOrEmpty(type.Namespace));
            foreach (var type in apps)
            {
                services.AddScoped(type, type);
            }
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //app.UseExceptionHandler("/Home/Error");
                app.UseMiddleware<NFine.Code.Middleware.ExceptionHandlerMiddleware>();
            }

            ///注册全局先上问关联的HttpContext
            System.Web.HttpContext.Configure(app.ApplicationServices.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(), env);

            //注册全局Configuration对象
            NFine.Code.ConfigurationManager.Configure(Configuration);

            app.UseSession();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "ExampleManage",
                    pattern: "ExampleManage/{controller}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "ReportManage",
                    pattern: "ReportManage/{controller}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                  name: "SystemManage",
                  pattern: "SystemManage/{controller}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                 name: "SystemSecurity",
                 pattern: "SystemSecurity/{controller}/{action=Index}/{id?}");
            });
        }
    }
}
