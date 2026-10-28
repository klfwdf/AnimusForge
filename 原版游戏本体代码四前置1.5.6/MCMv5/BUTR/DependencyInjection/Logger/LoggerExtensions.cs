using System;

namespace BUTR.DependencyInjection.Logger;

public static class LoggerExtensions
{
	public static void LogDebug(this IBUTRLogger logger, Exception exception, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Debug, message, args);
	}

	public static void LogDebug(this IBUTRLogger logger, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Debug, message, args);
	}

	public static void LogTrace(this IBUTRLogger logger, Exception exception, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Trace, message, args);
	}

	public static void LogTrace(this IBUTRLogger logger, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Trace, message, args);
	}

	public static void LogInformation(this IBUTRLogger logger, Exception exception, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Information, message, args);
	}

	public static void LogInformation(this IBUTRLogger logger, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Information, message, args);
	}

	public static void LogWarning(this IBUTRLogger logger, Exception exception, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Warning, message, args);
	}

	public static void LogWarning(this IBUTRLogger logger, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Warning, message, args);
	}

	public static void LogError(this IBUTRLogger logger, Exception exception, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Error, message, args);
	}

	public static void LogError(this IBUTRLogger logger, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Error, message, args);
	}

	public static void LogCritical(this IBUTRLogger logger, Exception exception, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Critical, message, args);
	}

	public static void LogCritical(this IBUTRLogger logger, string message, params object[] args)
	{
		if (logger == null)
		{
			throw new ArgumentNullException("logger");
		}
		logger.LogMessage(LogLevel.Critical, message, args);
	}
}
