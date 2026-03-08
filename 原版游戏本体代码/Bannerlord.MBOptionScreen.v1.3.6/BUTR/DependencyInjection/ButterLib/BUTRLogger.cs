using System;
using System.Runtime.CompilerServices;
using BUTR.DependencyInjection.Logger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BUTR.DependencyInjection.ButterLib
{
	// Token: 0x02000041 RID: 65
	[NullableContext(1)]
	[Nullable(0)]
	internal class BUTRLogger<[Nullable(2)] T> : IBUTRLogger<T>, IBUTRLogger
	{
		// Token: 0x0600021B RID: 539 RVA: 0x00008752 File Offset: 0x00006952
		public BUTRLogger(IServiceProvider serviceProvider)
		{
			this._logger = ServiceProviderServiceExtensions.GetRequiredService<ILogger<T>>(serviceProvider);
		}

		// Token: 0x0600021C RID: 540 RVA: 0x00008768 File Offset: 0x00006968
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

		// Token: 0x04000090 RID: 144
		private readonly ILogger _logger;
	}
}
