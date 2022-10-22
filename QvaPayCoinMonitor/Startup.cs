using System;
using System.IO;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QvaPayCoinMonitor.Configuration;


[assembly:FunctionsStartup(typeof(QvaPayCoinMonitor.Startup))]

namespace QvaPayCoinMonitor;

public class Startup : FunctionsStartup
{
    private static IConfiguration _configuration = null;
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        base.ConfigureAppConfiguration(builder);

        var context = builder.GetContext();

        builder.ConfigurationBuilder
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true,
                reloadOnChange: false)
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"),
                optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        var config = builder.GetContext().Configuration;

        builder.Services.AddOptions<QvaPayApiOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("QvaPayApi").Bind(settings);
            });

        builder.Services.AddHttpClient("QvaPayClient", client =>
        {
            client.BaseAddress = new Uri(config.GetValue<string>("QvaPayApi:BaseAddress"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("piqba/twitter-bot");
        });
    }
}