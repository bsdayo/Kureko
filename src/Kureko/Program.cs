using Kureko;
using Kureko.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostApplicationBuilder();

builder.Services.Configure<KurekoOptions>(builder.Configuration.GetSection("Kureko"));

var host = builder.Build();

await new KurekoBot(host).RunAsync();