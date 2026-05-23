using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Represents a trace writer that writes to the application's <see cref="T:System.Diagnostics.TraceListener" /> instances.
	/// </summary>
	// Token: 0x02000079 RID: 121
	public class DiagnosticsTraceWriter : ITraceWriter
	{
		/// <summary>
		/// Gets the <see cref="T:System.Diagnostics.TraceLevel" /> that will be used to filter the trace messages passed to the writer.
		/// For example a filter level of <see cref="F:System.Diagnostics.TraceLevel.Info" /> will exclude <see cref="F:System.Diagnostics.TraceLevel.Verbose" /> messages and include <see cref="F:System.Diagnostics.TraceLevel.Info" />,
		/// <see cref="F:System.Diagnostics.TraceLevel.Warning" /> and <see cref="F:System.Diagnostics.TraceLevel.Error" /> messages.
		/// </summary>
		/// <value>
		/// The <see cref="T:System.Diagnostics.TraceLevel" /> that will be used to filter the trace messages passed to the writer.
		/// </value>
		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x0600066B RID: 1643 RVA: 0x0001BE54 File Offset: 0x0001A054
		// (set) Token: 0x0600066C RID: 1644 RVA: 0x0001BE5C File Offset: 0x0001A05C
		public TraceLevel LevelFilter { get; set; }

		// Token: 0x0600066D RID: 1645 RVA: 0x0001BE65 File Offset: 0x0001A065
		private TraceEventType GetTraceEventType(TraceLevel level)
		{
			switch (level)
			{
			case TraceLevel.Error:
				return TraceEventType.Error;
			case TraceLevel.Warning:
				return TraceEventType.Warning;
			case TraceLevel.Info:
				return TraceEventType.Information;
			case TraceLevel.Verbose:
				return TraceEventType.Verbose;
			default:
				throw new ArgumentOutOfRangeException("level");
			}
		}

		/// <summary>
		/// Writes the specified trace level, message and optional exception.
		/// </summary>
		/// <param name="level">The <see cref="T:System.Diagnostics.TraceLevel" /> at which to write this trace.</param>
		/// <param name="message">The trace message.</param>
		/// <param name="ex">The trace exception. This parameter is optional.</param>
		// Token: 0x0600066E RID: 1646 RVA: 0x0001BE94 File Offset: 0x0001A094
		[NullableContext(1)]
		public void Trace(TraceLevel level, string message, [Nullable(2)] Exception ex)
		{
			if (level == TraceLevel.Off)
			{
				return;
			}
			TraceEventCache eventCache = new TraceEventCache();
			TraceEventType traceEventType = this.GetTraceEventType(level);
			foreach (object obj in System.Diagnostics.Trace.Listeners)
			{
				TraceListener traceListener = (TraceListener)obj;
				if (!traceListener.IsThreadSafe)
				{
					TraceListener obj2 = traceListener;
					lock (obj2)
					{
						traceListener.TraceEvent(eventCache, "Newtonsoft.Json", traceEventType, 0, message);
						goto IL_6E;
					}
					goto IL_5F;
				}
				goto IL_5F;
				IL_6E:
				if (System.Diagnostics.Trace.AutoFlush)
				{
					traceListener.Flush();
					continue;
				}
				continue;
				IL_5F:
				traceListener.TraceEvent(eventCache, "Newtonsoft.Json", traceEventType, 0, message);
				goto IL_6E;
			}
		}
	}
}
