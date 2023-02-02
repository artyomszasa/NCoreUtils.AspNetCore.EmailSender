using System;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if !DEBUG
using NCoreUtils.Logging;
#endif

namespace NCoreUtils.AspNetCore.EmailSender;

public class Startup
{
    private readonly IConfiguration _configuration;

    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var publisherTask = PublisherClient.CreateAsync(new TopicName(_configuration["Google:ProjectId"], _configuration["Google:TopicId"]));
        services
            .AddHttpClient()
            .AddHttpContextAccessor()
            .AddSingleton(_ => publisherTask.Result)
            .AddSingleton<IEmailSender, EmailScheduler>()
            .AddCors(b => b.AddDefaultPolicy(opts => opts
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                // must be at least 2 domains for CORS middleware to send Vary: Origin
                .WithOrigins("https://example.com", "http://127.0.0.1")
                .SetIsOriginAllowed(_ => true)
            ))
            .AddRemoteOAuth2Authentication(_configuration.GetSection("Authentication"), TokenHandlers.Bearer)
            .AddAuthorization()
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
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapEmailScheduler();
            });
    }
}
