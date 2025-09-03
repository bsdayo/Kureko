using System.CommandLine;
using System.Reflection;
using System.Text;
using Kureko.Infrastructure;
using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common.Interface.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kureko;

public sealed partial class KurekoBot
{
    public static string Version { get; }

#if DEBUG
    public static string Name { get; } = "Kureko/Debug";
#else
    public static string Name { get; } = "Kureko";
#endif

    public static string CommitHash { get; }

    static KurekoBot()
    {
        var versionParts = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            .Split('+');
        Version = versionParts?[0] ?? "unknown";
        CommitHash = versionParts is { Length: > 1 } ? versionParts[0] : new string('0', 40);
    }

    private readonly IHost _host;
    private readonly IOptionsMonitor<KurekoOptions> _kurekoOptions;
    private readonly ILogger<KurekoBot> _logger;
    private readonly BotRootCommand _rootCommand;

    public KurekoBot(IHost host)
    {
        _host = host;
        _kurekoOptions = _host.Services.GetRequiredService<IOptionsMonitor<KurekoOptions>>();
        _logger = _host.Services.GetRequiredService<ILogger<KurekoBot>>();
        _rootCommand = new BotRootCommand(host);
    }

    public async Task RunAsync()
    {
        var device = await BotCredentials.LoadOrCreateDeviceAsync();
        var keystore = await BotCredentials.LoadOrCreateKeystoreAsync();

        var config = new BotConfig();
        var bot = BotFactory.Create(config, device, keystore);

        // Register events
        bot.Invoker.OnBotLogEvent += OnBotLogEvent;
        bot.Invoker.OnBotCaptchaEvent += OnBotCaptchaEvent;
        bot.Invoker.OnBotNewDeviceVerify += OnBotNewDeviceVerify;
        bot.Invoker.OnBotOnlineEvent += OnBotOnlineEvent;
        bot.Invoker.OnFriendMessageReceived += OnFriendMessageReceived;
        bot.Invoker.OnGroupMessageReceived += OnGroupMessageReceived;

        // Login
        if (keystore.Uin == 0)
        {
            _logger.LogInformation("Logging in by QrCode");
            var qrcode = await bot.FetchQrCode();
            if (qrcode is not { } data) return;
            await File.WriteAllBytesAsync(BotConstants.LoginQrCodeFileName, data.QrCode);
            _logger.LogInformation("Login QrCode wrote to {FileName}", BotConstants.LoginQrCodeFileName);
            await bot.LoginByQrCode();
        }
        else
        {
            _logger.LogInformation("Logging in by password");
            await bot.LoginByPassword();
        }
    }

    private async Task<string?> ProcessCommandAsync(string input)
    {
        var result = _rootCommand.Parse(input);
        var outputWriter = new StringWriter();
        var errorWriter = new StringWriter();
        Exception? exception = null;

        try
        {
            await result.InvokeAsync(new InvocationConfiguration
            {
                EnableDefaultExceptionHandler = false,
                Output = outputWriter,
                Error = errorWriter
            });
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var responseBuilder = new StringBuilder();

        if (outputWriter.GetStringBuilder().Length > 0)
        {
            responseBuilder.AppendLine("[Output]");
            responseBuilder.Append(outputWriter.ToString().Trim());
        }

        if (errorWriter.GetStringBuilder().Length > 0)
        {
            if (responseBuilder.Length > 0) responseBuilder.Append("\n\n");
            responseBuilder.AppendLine("[Error]");
            responseBuilder.Append(errorWriter.ToString().Trim());
        }

        if (exception is not null)
        {
            if (responseBuilder.Length > 0) responseBuilder.Append("\n\n");
            responseBuilder.AppendLine("[Exception]");
            responseBuilder.Append(exception);
        }

        if (responseBuilder.Length == 0) return null;

        var response = responseBuilder.ToString().Trim();
        return response;
    }
}