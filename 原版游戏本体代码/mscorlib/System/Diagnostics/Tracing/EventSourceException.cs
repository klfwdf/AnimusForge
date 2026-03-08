using System;
using System.Runtime.Serialization;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000433 RID: 1075
	[__DynamicallyInvokable]
	[Serializable]
	public class EventSourceException : Exception
	{
		// Token: 0x0600358D RID: 13709 RVA: 0x000D1204 File Offset: 0x000CF404
		[__DynamicallyInvokable]
		public EventSourceException()
			: base(Environment.GetResourceString("EventSource_ListenerWriteFailure"))
		{
		}

		// Token: 0x0600358E RID: 13710 RVA: 0x000D1216 File Offset: 0x000CF416
		[__DynamicallyInvokable]
		public EventSourceException(string message)
			: base(message)
		{
		}

		// Token: 0x0600358F RID: 13711 RVA: 0x000D121F File Offset: 0x000CF41F
		[__DynamicallyInvokable]
		public EventSourceException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		// Token: 0x06003590 RID: 13712 RVA: 0x000D1229 File Offset: 0x000CF429
		protected EventSourceException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x06003591 RID: 13713 RVA: 0x000D1233 File Offset: 0x000CF433
		internal EventSourceException(Exception innerException)
			: base(Environment.GetResourceString("EventSource_ListenerWriteFailure"), innerException)
		{
		}
	}
}
