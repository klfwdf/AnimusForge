using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000461 RID: 1121
	internal sealed class BooleanArrayTypeInfo : TraceLoggingTypeInfo<bool[]>
	{
		// Token: 0x06003690 RID: 13968 RVA: 0x000D3529 File Offset: 0x000D1729
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format8(format, TraceLoggingDataType.Boolean8));
		}

		// Token: 0x06003691 RID: 13969 RVA: 0x000D353D File Offset: 0x000D173D
		public override void WriteData(TraceLoggingDataCollector collector, ref bool[] value)
		{
			collector.AddArray(value);
		}
	}
}
