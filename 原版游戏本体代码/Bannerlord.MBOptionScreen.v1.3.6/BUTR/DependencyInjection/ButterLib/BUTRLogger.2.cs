using System;
using System.Runtime.CompilerServices;
using BUTR.DependencyInjection.Logger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BUTR.DependencyInjection.ButterLib
{
	// Token: 0x02000042 RID: 66
	[NullableContext(1)]
	[Nullable(0)]
	internal class BUTRLogger : IBUTRLogger
	{
		// Token: 0x0600021D RID: 541 RVA: 0x000087E7 File Offset: 0x000069E7
		public BUTRLogger(IServiceProvider serviceProvider)
		{
			this._logger = ServiceProviderServiceExtensions.GetRequiredService<ILogger>(serviceProvider);
		}

		// Token: 0x0600021E RID: 542 RVA: 0x000087FC File Offset: 0x000069FC
		public void LogMessage(LogLevel logLevel, string message, params object[] args)
		{
			switch (logLevel)
			{
			case 0:
				LoggerExtensions.LogTrace(this._logger, message, args);
				return;
			case 1:
				LoggerExtensions.LogDebug(this._logger, message, args);
				return;
			case 2:
				LoggerExtensions.LogInformation(this._logger, message, args);
				return;
			case 3:
				LoggerExtensions.LogWarning(this._logger, message, args);
				return;
			case 4:
				LoggerExtensions.LogError(this._logger, message, args);
				return;
			case 5:
				LoggerExtensions.LogCritical(this._logger, message, args);
				return;
			default:
				return;
			}
		}

		// Token: 0x04000091 RID: 145
		private readonly ILogger _logger;
	}
}
