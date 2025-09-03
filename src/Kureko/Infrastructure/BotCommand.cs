using System.CommandLine;

namespace Kureko.Infrastructure;

public abstract class BotCommand : Command
{
    protected BotCommand(string name, string? description = null) : base(name, description)
    {
        SetAction(InvokeAsync);
    }

    private Task InvokeAsync(ParseResult parseResult)
    {
        using var activity = BotDiagnostics.StartActivity();
        return InvokeAsync(parseResult,
            parseResult.InvocationConfiguration.Output,
            parseResult.InvocationConfiguration.Error);
    }

    protected abstract Task InvokeAsync(ParseResult parseResult, TextWriter output, TextWriter error);
}