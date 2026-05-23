using System;
using System.Runtime.CompilerServices;
using BUTR.DependencyInjection.Logger;
using Microsoft.Extensions.Logging;

namespace MCM.UI.ButterLib
{
	// Token: 0x02000034 RID: 52
	[NullableContext(1)]
	[Nullable(0)]
	internal class LoggerWrapper : IBUTRLogger
	{
		// Token: 0x060001C5 RID: 453 RVA: 0x00007ECE File Offset: 0x000060CE
		public LoggerWrapper(ILogger logger)
		{
			this._logger = logger;
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x00007EE0 File Offset: 0x000060E0
		public void LogMessage(LogLevel logLevel, string message, params object[] args)
		{
			this._logger.Log<FormattedLogValues>(logLevel, default(EventId), new FormattedLogValues(message, args), null, (FormattedLogValues state, Exception _) => state.ToString());
		}

		// Token: 0x0400007B RID: 123
		private readonly ILogger _logger;
	}
}
