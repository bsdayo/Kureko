using LagrangeLogLevel = Lagrange.Core.Event.EventArg.LogLevel;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Kureko.Utilities;

public static class LogLevelConverter
{
    public static LogLevel Convert(LagrangeLogLevel level)
    {
        return level switch
        {
            LagrangeLogLevel.Debug => LogLevel.Debug,
            LagrangeLogLevel.Verbose => LogLevel.Trace,
            LagrangeLogLevel.Information => LogLevel.Information,
            LagrangeLogLevel.Warning => LogLevel.Warning,
            LagrangeLogLevel.Exception => LogLevel.Error,
            LagrangeLogLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.None
        };
    }
}