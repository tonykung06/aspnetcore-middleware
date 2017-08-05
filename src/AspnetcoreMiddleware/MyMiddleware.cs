using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace AspnetcoreMiddleware
{
    public class MyMiddleware
    {
        private RequestDelegate _next;
        private ILoggerFactory _loggerFactory;
        private IOptions<MyMiddlewareOptions> _options;

        public MyMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IOptions<MyMiddlewareOptions> options)
        {
            _options = options;
            _loggerFactory = loggerFactory;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            _loggerFactory.AddConsole();
            var logger = _loggerFactory.CreateLogger("MyMiddleware");
            logger.LogInformation("testing", _options.Value.OptionOne);

            // used for passing data from one middleware to other middlewares
            context.Items["message_from_MyMiddleware"] = "hey from MyMiddleware";

            await context.Response.WriteAsync($"hello from MyMiddleware, {_options.Value.OptionOne}, ");
            await _next.Invoke(context);
        }
    }

    public class MyMiddlewareOptions
    {
        public string OptionOne { get; set; }
    }

    public static class MyMiddlewareExtensions
    {
        public static IApplicationBuilder UseMyMiddleware(this IApplicationBuilder appBuilder)
        {
            return appBuilder.UseMiddleware<MyMiddleware>();
        }
    }
}
