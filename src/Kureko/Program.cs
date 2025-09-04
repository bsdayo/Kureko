using System.CommandLine;
using Kureko;
using Kureko.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var rootCmd = new RootCommand("Private QQ bot");

var runCmd = new Command("run", "Run the bot");
rootCmd.Subcommands.Add(runCmd);
{
    var configOpt = new Option<FileInfo>("-c", "--config")
        { Description = "Path to configuration file" };
    runCmd.Options.Add(configOpt);

    runCmd.SetAction(async parseResult =>
    {
        var builder = new HostApplicationBuilder();

        builder.Configuration.Sources.Clear();
        if (parseResult.GetValue(configOpt) is { } configFile)
            builder.Configuration.AddJsonFile(configFile.FullName, true, true);

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: BotDiagnostics.Name, serviceVersion: BotDiagnostics.Version))
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddMeter(BotDiagnostics.Name))
            .WithTracing(tracing => tracing
                .AddSource(BotDiagnostics.Name))
            .UseOtlpExporter();

        builder.Services.Configure<BotOptions>(builder.Configuration.GetSection("Bot"));

        var host = builder.Build();
        var kureko = new KurekoBot(host);

        await kureko.RunAsync();
    });
}

return await rootCmd.Parse(args).InvokeAsync();