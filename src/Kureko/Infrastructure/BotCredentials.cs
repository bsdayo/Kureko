using System.Text.Json;
using Lagrange.Core.Common;
using Lagrange.Core.Event.EventArg;

namespace Kureko.Infrastructure;

public static class BotCredentials
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static async Task<BotDeviceInfo> LoadOrCreateDeviceAsync()
    {
        if (File.Exists(BotConstants.DeviceFileName))
        {
            var deviceJson = await File.ReadAllTextAsync(BotConstants.DeviceFileName);
            return JsonSerializer.Deserialize<BotDeviceInfo>(deviceJson)
                   ?? throw new InvalidOperationException();
        }

        var device = BotDeviceInfo.GenerateInfo();
        device.DeviceName = "Kureko";
        await File.WriteAllTextAsync(BotConstants.DeviceFileName,
            JsonSerializer.Serialize(device, SerializerOptions));
        return device;
    }

    public static async Task<BotKeystore> LoadOrCreateKeystoreAsync()
    {
        if (File.Exists(BotConstants.KeystoreFileName))
        {
            var keystoreJson = await File.ReadAllTextAsync(BotConstants.KeystoreFileName);
            return JsonSerializer.Deserialize<BotKeystore>(keystoreJson)
                   ?? throw new InvalidOperationException();
        }

        var keystore = new BotKeystore();
        await SaveKeystoreAsync(keystore);
        return keystore;
    }

    public static async Task SaveKeystoreAsync(BotKeystore keystore)
    {
        await File.WriteAllTextAsync(BotConstants.KeystoreFileName,
            JsonSerializer.Serialize(keystore, SerializerOptions));
    }
}