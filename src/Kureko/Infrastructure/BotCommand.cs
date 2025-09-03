using System.CommandLine;

namespace Kureko.Infrastructure;

public abstract class BotCommand : Command
{
    protected BotCommand(string name, string? description = null) : base(name, description)
    {
        SetAction(result => InvokeAsync(result,
            result.InvocationConfiguration.Output,
            result.InvocationConfiguration.Error));
    }

    protected abstract Task InvokeAsync(ParseResult parseResult, TextWriter output, TextWriter error);
}