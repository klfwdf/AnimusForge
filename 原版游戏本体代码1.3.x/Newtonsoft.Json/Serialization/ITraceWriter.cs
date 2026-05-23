using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Represents a trace writer.
	/// </summary>
	// Token: 0x02000082 RID: 130
	[NullableContext(1)]
	public interface ITraceWriter
	{
		/// <summary>
		/// Gets the <see cref="T:System.Diagnostics.TraceLevel" /> that will be used to filter the trace messages passed to the writer.
		/// For example a filter level of <see cref="F:System.Diagnostics.TraceLevel.Info" /> will exclude <see cref="F:System.Diagnostics.TraceLevel.Verbose" /> messages and include <see cref="F:System.Diagnostics.TraceLevel.Info" />,
		/// <see cref="F:System.Diagnostics.TraceLevel.Warning" /> and <see cref="F:System.Diagnostics.TraceLevel.Error" /> messages.
		/// </summary>
		/// <value>The <see cref="T:System.Diagnostics.TraceLevel" /> that will be used to filter the trace messages passed to the writer.</value>
		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x0600068B RID: 1675
		TraceLevel LevelFilter { get; }

		/// <summary>
		/// Writes the specified trace level, message and optional exception.
		/// </summary>
		/// <param name="level">The <see cref="T:System.Diagnostics.TraceLevel" /> at which to write this trace.</param>
		/// <param name="message">The trace message.</param>
		/// <param name="ex">The trace exception. This parameter is optional.</param>
		// Token: 0x0600068C RID: 1676
		void Trace(TraceLevel level, string message, [Nullable(2)] Exception ex);
	}
}
