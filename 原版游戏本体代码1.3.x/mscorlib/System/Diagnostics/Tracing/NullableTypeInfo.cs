using System;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200047F RID: 1151
	internal sealed class NullableTypeInfo<T> : TraceLoggingTypeInfo<T?> where T : struct
	{
		// Token: 0x060036F4 RID: 14068 RVA: 0x000D3BA7 File Offset: 0x000D1DA7
		public NullableTypeInfo(List<Type> recursionCheck)
		{
			this.valueInfo = TraceLoggingTypeInfo<T>.GetInstance(recursionCheck);
		}

		// Token: 0x060036F5 RID: 14069 RVA: 0x000D3BBC File Offset: 0x000D1DBC
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			TraceLoggingMetadataCollector traceLoggingMetadataCollector = collector.AddGroup(name);
			traceLoggingMetadataCollector.AddScalar("HasValue", TraceLoggingDataType.Boolean8);
			this.valueInfo.WriteMetadata(traceLoggingMetadataCollector, "Value", format);
		}

		// Token: 0x060036F6 RID: 14070 RVA: 0x000D3BF4 File Offset: 0x000D1DF4
		public override void WriteData(TraceLoggingDataCollector collector, ref T? value)
		{
			bool flag = value != null;
			collector.AddScalar(flag);
			T t = (flag ? value.Value : default(T));
			this.valueInfo.WriteData(collector, ref t);
		}

		// Token: 0x0400185F RID: 6239
		private readonly TraceLoggingTypeInfo<T> valueInfo;
	}
}
