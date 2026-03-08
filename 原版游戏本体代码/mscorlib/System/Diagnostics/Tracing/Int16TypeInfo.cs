using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000456 RID: 1110
	internal sealed class Int16TypeInfo : TraceLoggingTypeInfo<short>
	{
		// Token: 0x0600366F RID: 13935 RVA: 0x000D33A3 File Offset: 0x000D15A3
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format16(format, TraceLoggingDataType.Int16));
		}

		// Token: 0x06003670 RID: 13936 RVA: 0x000D33B3 File Offset: 0x000D15B3
		public override void WriteData(TraceLoggingDataCollector collector, ref short value)
		{
			collector.AddScalar(value);
		}
	}
}
