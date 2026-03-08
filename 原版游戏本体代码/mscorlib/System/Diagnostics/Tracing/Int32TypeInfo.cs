using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000458 RID: 1112
	internal sealed class Int32TypeInfo : TraceLoggingTypeInfo<int>
	{
		// Token: 0x06003675 RID: 13941 RVA: 0x000D33E7 File Offset: 0x000D15E7
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format32(format, TraceLoggingDataType.Int32));
		}

		// Token: 0x06003676 RID: 13942 RVA: 0x000D33F7 File Offset: 0x000D15F7
		public override void WriteData(TraceLoggingDataCollector collector, ref int value)
		{
			collector.AddScalar(value);
		}
	}
}
