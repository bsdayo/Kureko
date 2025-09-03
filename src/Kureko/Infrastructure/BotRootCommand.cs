using System.CommandLine;
using Kureko.Commands;
using Lagrange.Core;
using Microsoft.Extensions.Hosting;

namespace Kureko.Infrastructure;

public class BotRootCommand : RootCommand
{
    public BotRootCommand(IHost host, BotContext bot) : base("Private QQ bot")
    {
        Subcommands.Add(new PingCommand());
    }
}