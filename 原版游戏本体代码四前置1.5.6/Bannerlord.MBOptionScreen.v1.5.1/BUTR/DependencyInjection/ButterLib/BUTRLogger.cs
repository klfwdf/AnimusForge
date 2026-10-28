using System;
using BUTR.DependencyInjection.Logger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BUTR.DependencyInjection.ButterLib;

internal class BUTRLogger<T> : IBUTRLogger<T>, IBUTRLogger
{
	private readonly ILogger _logger;

	public BUTRLogger(IServiceProvider serviceProvider)
	{
		_logger = serviceProvider.GetRequiredService<ILogger<T>>();
	}

	public void LogMessage(BUTR.DependencyInjection.Logger.LogLevel logLevel, string message, params object[] args)
	{
		switch (logLevel)
		{
		case BUTR.DependencyInjection.Logger.LogLevel.Trace:
			_logger.LogTrace(message, args);
			break;
		case BUTR.DependencyInjection.Logger.LogLevel.Debug:
			_logger.LogDebug(message, args);
			break;
		case BUTR.DependencyInjection.Logger.LogLevel.Information:
			_logger.LogInformation(message, args);
			break;
		case BUTR.DependencyInjection.Logger.LogLevel.Warning:
			_logger.LogWarning(message, args);
			break;
		case BUTR.DependencyInjection.Logger.LogLevel.Error:
			_logger.LogError(message, args);
			break;
		case BUTR.DependencyInjection.Logger.LogLevel.Critical:
			_logger.LogCritical(message, args);
			break;
		}
	}
}
internal class BUTRLogger : IBUTRLogger
{
	private readonly ILogger _logger;

	public BUTRLogger(IServiceProvider serviceProvider)
	{
		_logger = serviceProvider.GetRequiredService<ILogger>();
	}

	public void LogMessage(BUTR.DependencyInjection.Logger.LogLevel logLevel, string message, params object[] args)
	{
		switch (logLevel)
		{
		case BUTR.DependencyInjection.Logger.LogLevel.Trace:
			_logger.LogTrace(message, args);
			break;
		case BUTR.DependencyInjection.Logger.LogLevel.Debug:
			_logger.LogDebug(message, args);
			break;
		case BUTR.DependencyInjection.Logger.LogLevel.Information:
			_logger.LogInformation(message, args);
			break;
		case BUTR.DependencyInjection.Logger.LogLevel.Warning:
			_logger.LogWarning(message, args);
			break;
		case BUTR.DependencyInjection.Logger.LogLevel.Error:
			_logger.LogError(message, args);
			break;
		case BUTR.DependencyInjection.Logger.LogLevel.Critical:
			_logger.LogCritical(message, args);
			break;
		}
	}
}
