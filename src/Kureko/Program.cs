using Kureko;
using Kureko.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = new HostApplicationBuilder();

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