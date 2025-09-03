using System.CommandLine;
using Kureko.Infrastructure;

namespace Kureko.Commands;

public class PingCommand() : BotCommand("ping")
{
    protected override Task InvokeAsync(ParseResult parseResult, TextWriter output, TextWriter error)
    {
        output.WriteLine("pong");
        return Task.CompletedTask;
    }
}