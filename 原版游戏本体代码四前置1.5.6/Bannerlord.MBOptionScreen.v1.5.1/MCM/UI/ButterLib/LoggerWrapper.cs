using System;
using BUTR.DependencyInjection.Logger;
using Microsoft.Extensions.Logging;

namespace MCM.UI.ButterLib;

internal class LoggerWrapper : IBUTRLogger
{
	private readonly ILogger _logger;

	public LoggerWrapper(ILogger logger)
	{
		_logger = logger;
	}

	public void LogMessage(BUTR.DependencyInjection.Logger.LogLevel logLevel, string message, params object[] args)
	{
		_logger.Log((Microsoft.Extensions.Logging.LogLevel)logLevel, default(EventId), new FormattedLogValues(message, args), null, (FormattedLogValues state, Exception _) => state.ToString());
	}
}
internal class LoggerWrapper<T> : IBUTRLogger<T>, IBUTRLogger
{
	private readonly ILogger<T> _logger;

	public LoggerWrapper(ILogger<T> logger)
	{
		_logger = logger;
	}

	public void LogMessage(BUTR.DependencyInjection.Logger.LogLevel logLevel, string message, params object[] args)
	{
		_logger.Log((Microsoft.Extensions.Logging.LogLevel)logLevel, default(EventId), new FormattedLogValues(message, args), null, (FormattedLogValues state, Exception _) => state.ToString());
	}
}
