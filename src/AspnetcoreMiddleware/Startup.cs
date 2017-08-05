using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Routing;

namespace AspnetcoreMiddleware
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(env.ContentRootPath)
                            .AddJsonFile("appsettings.json", false, true);
            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddCors();
            services.AddRouting();
            services.AddDistributedMemoryCache();
            services.AddSession();
            
            var myMiddlewareOptions = Configuration.GetSection("MyMiddlewareSection");
            services.Configure<MyMiddlewareOptions>(option => option.OptionOne = myMiddlewareOptions["OptionOne"]);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseSession();

            var routeBuilder = new RouteBuilder(app);
            routeBuilder.MapGet("greeting/{name}", appBuilder =>
            {
                appBuilder.Run(async context => {
                    await context.Response.WriteAsync($"greeting to {context.GetRouteValue("name")}");
                });
            });
            app.UseRouter(routeBuilder.Build());

            app.UseCors(policyBuilder =>
            {
                policyBuilder.WithOrigins("http://localhost:5000");
            });

            app.Use(async (context, next) =>
            {
                context.Session.SetString("session_var_1", "session_var_1_value");

                await context.Response.WriteAsync("First middleware starts, ");
                await next.Invoke();
                await context.Response.WriteAsync("First middleware ends, ");
            });

            app.Map("/mymapbranch", (appBuilder) =>
            {
                appBuilder.Use(async (context, next) =>
                {
                    await context.Response.WriteAsync("Second middleware starts, ");
                    await next.Invoke();
                    await context.Response.WriteAsync("Second middleware ends, ");
                });
                appBuilder.Run(async (context) =>
                {
                    context.Response.WriteAsync("hey from mymapbranch, ");
                });
            });

            app.MapWhen(context => context.Request.Query.ContainsKey("mymapwhen"), (appBuilder) =>
            {
                appBuilder.Use(async (context, next) =>
                {
                    await context.Response.WriteAsync("Third middleware starts, ");
                    await next.Invoke();
                    await context.Response.WriteAsync("Third middleware ends, ");
                });
                appBuilder.Run(async (context) =>
                {
                    context.Response.WriteAsync("hey from mymapwhen, ");
                });
            });

            app.UseMyMiddleware();

            // app.Run is pipeline terminator
            // this will never be executed when it branches out into /mymapbranch
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync(
                    $"Hello World!, {context.Items["message_from_MyMiddleware"]}, {context.Session.GetString("session_var_1")}, "
                );
            });
        }
    }
}
