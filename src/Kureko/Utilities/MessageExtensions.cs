using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;

namespace Kureko.Utilities;

public static class MessageChainExtensions
{
    public static string GetText(this MessageChain chain)
    {
        return string.Join('\n', chain.OfType<TextEntity>().Select(entity => entity.Text));
    }
}