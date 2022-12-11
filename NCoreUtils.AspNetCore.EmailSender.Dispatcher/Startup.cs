using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if !DEBUG
using NCoreUtils.Logging;
#endif

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class Startup
    {
        static ForwardedHeadersOptions ConfigureForwardedHeaders()
        {
            var opts = new ForwardedHeadersOptions();
            opts.KnownNetworks.Clear();
            opts.KnownProxies.Clear();
            opts.ForwardedHeaders = ForwardedHeaders.All;
            return opts;
        }

        private readonly IConfiguration _configuration;

        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddHttpContextAccessor()
                .AddHttpClient()
                .AddSingleton<EmailProcessor>()
                .AddSingleton(serviceProvider => DispatcherConfig.FromConfiguration(serviceProvider, _configuration.GetSection("Dispatchers")))
                .AddCors(b => b.AddDefaultPolicy(opts => opts
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    // must be at least 2 domains for CORS middleware to send Vary: Origin
                    .WithOrigins("https://example.com", "http://127.0.0.1")
                    .SetIsOriginAllowed(_ => true)
                ))
                .AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
#if DEBUG
            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
#endif

            app
                .UseForwardedHeaders(_configuration.GetSection("ForwardedHeaders"))
#if !DEBUG
                .UsePrePopulateLoggingContext()
#endif
                .UseCors()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapPost("/", context =>
                    {
                        var processor = context.RequestServices.GetRequiredService<EmailProcessor>();
                        return processor.ProcessRequestAsync(context);
                    });
                });
        }
    }
}
