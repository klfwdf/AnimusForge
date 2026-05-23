using System;
using System.Threading;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000451 RID: 1105
	internal class SimpleEventTypes<T> : TraceLoggingEventTypes
	{
		// Token: 0x0600365F RID: 13919 RVA: 0x000D32C2 File Offset: 0x000D14C2
		private SimpleEventTypes(TraceLoggingTypeInfo<T> typeInfo)
			: base(typeInfo.Name, typeInfo.Tags, new TraceLoggingTypeInfo[] { typeInfo })
		{
			this.typeInfo = typeInfo;
		}

		// Token: 0x1700080C RID: 2060
		// (get) Token: 0x06003660 RID: 13920 RVA: 0x000D32E7 File Offset: 0x000D14E7
		public static SimpleEventTypes<T> Instance
		{
			get
			{
				return SimpleEventTypes<T>.instance ?? SimpleEventTypes<T>.InitInstance();
			}
		}

		// Token: 0x06003661 RID: 13921 RVA: 0x000D32F8 File Offset: 0x000D14F8
		private static SimpleEventTypes<T> InitInstance()
		{
			SimpleEventTypes<T> value = new SimpleEventTypes<T>(TraceLoggingTypeInfo<T>.Instance);
			Interlocked.CompareExchange<SimpleEventTypes<T>>(ref SimpleEventTypes<T>.instance, value, null);
			return SimpleEventTypes<T>.instance;
		}

		// Token: 0x0400185B RID: 6235
		private static SimpleEventTypes<T> instance;

		// Token: 0x0400185C RID: 6236
		internal readonly TraceLoggingTypeInfo<T> typeInfo;
	}
}
