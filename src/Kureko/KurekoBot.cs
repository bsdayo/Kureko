using System.CommandLine;
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

public sealed partial class KurekoBot : IHost
{
    private readonly IHost _host;
    private readonly IOptionsMonitor<BotOptions> _kurekoOptions;
    private readonly ILogger<KurekoBot> _logger;

    private BotContext? _bot;
    private BotRootCommand? _rootCommand;

    public IServiceProvider Services => _host.Services;

    public KurekoBot(IHost host)
    {
        _host = host;
        _kurekoOptions = _host.Services.GetRequiredService<IOptionsMonitor<BotOptions>>();
        _logger = _host.Services.GetRequiredService<ILogger<KurekoBot>>();
    }

    private static async Task<BotContext> GetBotAsync()
    {
        var config = new BotConfig();
        var device = await BotCredentials.LoadOrCreateDeviceAsync();
        var keystore = await BotCredentials.LoadOrCreateKeystoreAsync();
        return BotFactory.Create(config, device, keystore);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _bot = await GetBotAsync();
        _rootCommand = new BotRootCommand(_host, _bot);

        // Register events
        _bot.Invoker.OnBotLogEvent += OnBotLogEvent;
        _bot.Invoker.OnBotCaptchaEvent += OnBotCaptchaEvent;
        _bot.Invoker.OnBotNewDeviceVerify += OnBotNewDeviceVerify;
        _bot.Invoker.OnBotOnlineEvent += OnBotOnlineEvent;
        _bot.Invoker.OnFriendMessageReceived += OnFriendMessageReceived;
        _bot.Invoker.OnGroupMessageReceived += OnGroupMessageReceived;

        // Login
        if (_bot.BotUin == 0)
        {
            _logger.LogInformation("Logging in by QrCode");
            var qrcode = await _bot.FetchQrCode();
            if (qrcode is not { } data) return;
            await File.WriteAllBytesAsync(BotConstants.LoginQrCodeFileName, data.QrCode, cancellationToken);
            _logger.LogInformation("Login QrCode wrote to {FileName}", BotConstants.LoginQrCodeFileName);
            await _bot.LoginByQrCode(cancellationToken);
        }
        else
        {
            _logger.LogInformation("Logging in by password");
            await _bot.LoginByPassword(cancellationToken);
        }

        await _host.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _host.StopAsync(cancellationToken);

        if (_bot is null) return;
        _bot.Dispose();
        _bot = null;
    }

    public void Dispose()
    {
        _host.Dispose();
    }

    private async Task<string?> ProcessCommandAsync(string input)
    {
        using var activity = BotDiagnostics.StartActivity();

        if (_rootCommand is null)
            throw new InvalidOperationException("Bot is not started.");

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