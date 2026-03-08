using System;
using System.Runtime.CompilerServices;
using BUTR.DependencyInjection.Logger;
using Microsoft.Extensions.Logging;

namespace MCM.UI.ButterLib
{
	// Token: 0x02000035 RID: 53
	[NullableContext(1)]
	[Nullable(0)]
	internal class LoggerWrapper<[Nullable(2)] T> : IBUTRLogger<T>, IBUTRLogger
	{
		// Token: 0x060001C7 RID: 455 RVA: 0x00007F29 File Offset: 0x00006129
		public LoggerWrapper(ILogger<T> logger)
		{
			this._logger = logger;
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x00007F38 File Offset: 0x00006138
		public void LogMessage(LogLevel logLevel, string message, params object[] args)
		{
			this._logger.Log<FormattedLogValues>(logLevel, default(EventId), new FormattedLogValues(message, args), null, (FormattedLogValues state, Exception _) => state.ToString());
		}

		// Token: 0x0400007C RID: 124
		private readonly ILogger<T> _logger;
	}
}
