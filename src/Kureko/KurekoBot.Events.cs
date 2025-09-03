using System.Net.Sockets;
using Kureko.Infrastructure;
using Kureko.Utilities;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.Logging;

namespace Kureko;

public partial class KurekoBot
{
    private void OnBotLogEvent(BotContext _, BotLogEvent log)
    {
        _logger.Log(LogLevelConverter.Convert(log.Level), "({Tag}) {Message}", log.Tag, log.EventMessage);
    }

    private void OnBotCaptchaEvent(BotContext bot, BotCaptchaEvent captcha)
    {
        _logger.LogWarning("Captcha required: {Url}", captcha.Url);

        Task.Run(async () =>
        {
            using var listener = TcpListener.Create(BotConstants.CaptchaListenerPort);
            listener.Start();

            _logger.LogInformation("Captcha listener started on port {Port}", BotConstants.CaptchaListenerPort);

            var client = await listener.AcceptTcpClientAsync();
            var stream = client.GetStream();

            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream) { AutoFlush = true };

            await writer.WriteLineAsync("Enter ticket:");
            var ticket = await reader.ReadLineAsync();
            await writer.WriteLineAsync("Enter randStr:");
            var randStr = await reader.ReadLineAsync();

            client.Close();

            if (string.IsNullOrWhiteSpace(ticket) || string.IsNullOrWhiteSpace(randStr))
            {
                _logger.LogWarning("Invalid captcha response");
            }
            else
            {
                _logger.LogInformation("Captcha response received");
                bot.SubmitCaptcha(ticket, randStr);
            }
        });
    }

    private void OnBotNewDeviceVerify(BotContext bot, BotNewDeviceVerifyEvent verify)
    {
        Task.Run(async () =>
        {
            await File.WriteAllBytesAsync(BotConstants.NewDeviceQrCodeFileName, verify.QrCode);
            _logger.LogInformation("New device verification QRCode wrote to {FileName}",
                BotConstants.NewDeviceQrCodeFileName);
        });
    }

    private void OnBotOnlineEvent(BotContext bot, BotOnlineEvent _)
    {
        Task.Run(async () =>
        {
            await BotCredentials.SaveKeystoreAsync(bot.UpdateKeystore());
            _logger.LogInformation("Keystore updated.");
        });
    }

    private void OnFriendMessageReceived(BotContext bot, FriendMessageEvent message)
    {
        var activity = BotDiagnostics.StartActivity();
        if (message.Chain.FriendUin == bot.BotUin)
            return;
        if (!message.Chain.HasTypeOf<TextEntity>())
            return;

        var input = message.Chain.GetText();
        var prefix = _kurekoOptions.CurrentValue.CommandPrefix;
        if (!input.StartsWith(prefix))
            return;
        input = input.TrimStart(prefix);
        if (string.IsNullOrWhiteSpace(input))
            return;

        Task.Run(async () =>
        {
            var response = await ProcessCommandAsync(input);
            if (response is not null)
                await bot.SendMessage(MessageBuilder.Friend(message.Chain.FriendUin)
                    .Text(response)
                    .Build());
            activity?.Dispose();
        });
    }

    private void OnGroupMessageReceived(BotContext bot, GroupMessageEvent message)
    {
        var activity = BotDiagnostics.StartActivity();
        if (message.Chain.FriendUin == bot.BotUin)
            return;
        if (!message.Chain.HasTypeOf<TextEntity>())
            return;

        var input = message.Chain.GetText();
        var prefix = _kurekoOptions.CurrentValue.CommandPrefix;
        if (!input.StartsWith(prefix))
            return;
        input = input.TrimStart(prefix);
        if (string.IsNullOrWhiteSpace(input))
            return;

        Task.Run(async () =>
        {
            var response = await ProcessCommandAsync(input);
            if (response is not null)
                await bot.SendMessage(MessageBuilder.Group(message.Chain.GroupUin!.Value)
                    .Text(response)
                    .Build());
            activity?.Dispose();
        });
    }
}