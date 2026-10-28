#define TRACE
using System.Diagnostics;

namespace BUTR.DependencyInjection.Logger;

public class DefaultBUTRLogger : IBUTRLogger
{
	public void LogMessage(LogLevel logLevel, string message, params object[] args)
	{
		switch (logLevel)
		{
		default:
			Trace.TraceInformation(message, args);
			break;
		case LogLevel.Information:
			Trace.TraceInformation(message, args);
			break;
		case LogLevel.Warning:
			Trace.TraceWarning(message, args);
			break;
		case LogLevel.Error:
			Trace.TraceError(message, args);
			break;
		case LogLevel.Critical:
			Trace.TraceError(message, args);
			break;
		}
	}
}
public class DefaultBUTRLogger<T> : IBUTRLogger<T>, IBUTRLogger
{
	public void LogMessage(LogLevel logLevel, string message, params object[] args)
	{
		switch (logLevel)
		{
		default:
			Trace.TraceInformation(message, args);
			break;
		case LogLevel.Information:
			Trace.TraceInformation(message, args);
			break;
		case LogLevel.Warning:
			Trace.TraceWarning(message, args);
			break;
		case LogLevel.Error:
			Trace.TraceError(message, args);
			break;
		case LogLevel.Critical:
			Trace.TraceError(message, args);
			break;
		}
	}
}
