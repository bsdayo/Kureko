using System.CommandLine;
using Kureko.Commands;
using Microsoft.Extensions.Hosting;

namespace Kureko.Infrastructure;

public class BotRootCommand : RootCommand
{
    public BotRootCommand(IHost host) : base("Private QQ bot")
    {
        Subcommands.Add(new PingCommand());
    }
}