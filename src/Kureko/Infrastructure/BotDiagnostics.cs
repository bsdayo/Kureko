using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Tag = System.Collections.Generic.KeyValuePair<string, object?>;

namespace Kureko.Infrastructure;

public static class BotDiagnostics
{
    public static string Name => "Kureko";

    public static string? Version { get; } = typeof(Program).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion;

    private static readonly ActivitySource ActivitySource = new(Name, Version);

    public static Activity? StartActivity(
        [CallerMemberName] string name = "",
        ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(name, kind);
    }

    private static readonly Meter Meter = new(Name, Version);

    private static Counter<long> ExecutedCommands { get; } = Meter.CreateCounter<long>(
        name: "kureko.executed_commands",
        description: "Number of commands executed");

    public static void RecordCommandExecution(string commandName, bool succeeded)
    {
        ExecutedCommands.Add(1, new TagList
        {
            new Tag("command", commandName),
            new Tag("state", succeeded ? "succeeded" : "failed"),
        });
    }
}