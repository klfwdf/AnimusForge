using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class LogEntry
{
	public LogLevel Level { get; private set; }

	public string Message { get; private set; }

	public LogEntry(LogLevel level, string message)
	{
		Level = level;
		Message = message;
	}
}
