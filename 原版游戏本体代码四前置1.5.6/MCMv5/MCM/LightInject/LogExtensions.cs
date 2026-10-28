using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class LogExtensions
{
	public static void Info(this Action<LogEntry> logAction, string message)
	{
		logAction(new LogEntry(LogLevel.Info, message));
	}

	public static void Warning(this Action<LogEntry> logAction, string message)
	{
		logAction(new LogEntry(LogLevel.Warning, message));
	}
}
