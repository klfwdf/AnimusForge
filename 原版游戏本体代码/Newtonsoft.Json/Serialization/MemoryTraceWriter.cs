using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Represents a trace writer that writes to memory. When the trace message limit is
	/// reached then old trace messages will be removed as new messages are added.
	/// </summary>
	// Token: 0x0200009C RID: 156
	[NullableContext(1)]
	[Nullable(0)]
	public class MemoryTraceWriter : ITraceWriter
	{
		/// <summary>
		/// Gets the <see cref="T:System.Diagnostics.TraceLevel" /> that will be used to filter the trace messages passed to the writer.
		/// For example a filter level of <see cref="F:System.Diagnostics.TraceLevel.Info" /> will exclude <see cref="F:System.Diagnostics.TraceLevel.Verbose" /> messages and include <see cref="F:System.Diagnostics.TraceLevel.Info" />,
		/// <see cref="F:System.Diagnostics.TraceLevel.Warning" /> and <see cref="F:System.Diagnostics.TraceLevel.Error" /> messages.
		/// </summary>
		/// <value>
		/// The <see cref="T:System.Diagnostics.TraceLevel" /> that will be used to filter the trace messages passed to the writer.
		/// </value>
		// Token: 0x17000160 RID: 352
		// (get) Token: 0x06000823 RID: 2083 RVA: 0x00023EB9 File Offset: 0x000220B9
		// (set) Token: 0x06000824 RID: 2084 RVA: 0x00023EC1 File Offset: 0x000220C1
		public TraceLevel LevelFilter { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.MemoryTraceWriter" /> class.
		/// </summary>
		// Token: 0x06000825 RID: 2085 RVA: 0x00023ECA File Offset: 0x000220CA
		public MemoryTraceWriter()
		{
			this.LevelFilter = TraceLevel.Verbose;
			this._traceMessages = new Queue<string>();
			this._lock = new object();
		}

		/// <summary>
		/// Writes the specified trace level, message and optional exception.
		/// </summary>
		/// <param name="level">The <see cref="T:System.Diagnostics.TraceLevel" /> at which to write this trace.</param>
		/// <param name="message">The trace message.</param>
		/// <param name="ex">The trace exception. This parameter is optional.</param>
		// Token: 0x06000826 RID: 2086 RVA: 0x00023EF0 File Offset: 0x000220F0
		public void Trace(TraceLevel level, string message, [Nullable(2)] Exception ex)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff", CultureInfo.InvariantCulture));
			stringBuilder.Append(" ");
			stringBuilder.Append(level.ToString("g"));
			stringBuilder.Append(" ");
			stringBuilder.Append(message);
			string item = stringBuilder.ToString();
			object @lock = this._lock;
			lock (@lock)
			{
				if (this._traceMessages.Count >= 1000)
				{
					this._traceMessages.Dequeue();
				}
				this._traceMessages.Enqueue(item);
			}
		}

		/// <summary>
		/// Returns an enumeration of the most recent trace messages.
		/// </summary>
		/// <returns>An enumeration of the most recent trace messages.</returns>
		// Token: 0x06000827 RID: 2087 RVA: 0x00023FB4 File Offset: 0x000221B4
		public IEnumerable<string> GetTraceMessages()
		{
			return this._traceMessages;
		}

		/// <summary>
		/// Returns a <see cref="T:System.String" /> of the most recent trace messages.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String" /> of the most recent trace messages.
		/// </returns>
		// Token: 0x06000828 RID: 2088 RVA: 0x00023FBC File Offset: 0x000221BC
		public override string ToString()
		{
			object @lock = this._lock;
			string result;
			lock (@lock)
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (string value in this._traceMessages)
				{
					if (stringBuilder.Length > 0)
					{
						stringBuilder.AppendLine();
					}
					stringBuilder.Append(value);
				}
				result = stringBuilder.ToString();
			}
			return result;
		}

		// Token: 0x040002DD RID: 733
		private readonly Queue<string> _traceMessages;

		// Token: 0x040002DE RID: 734
		private readonly object _lock;
	}
}
