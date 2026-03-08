using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000453 RID: 1107
	internal sealed class BooleanTypeInfo : TraceLoggingTypeInfo<bool>
	{
		// Token: 0x06003666 RID: 13926 RVA: 0x000D3339 File Offset: 0x000D1539
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format8(format, TraceLoggingDataType.Boolean8));
		}

		// Token: 0x06003667 RID: 13927 RVA: 0x000D334D File Offset: 0x000D154D
		public override void WriteData(TraceLoggingDataCollector collector, ref bool value)
		{
			collector.AddScalar(value);
		}
	}
}
