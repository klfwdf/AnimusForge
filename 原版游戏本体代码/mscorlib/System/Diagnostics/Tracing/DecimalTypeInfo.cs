using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200047D RID: 1149
	internal sealed class DecimalTypeInfo : TraceLoggingTypeInfo<decimal>
	{
		// Token: 0x060036ED RID: 14061 RVA: 0x000D3A84 File Offset: 0x000D1C84
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.Double, format));
		}

		// Token: 0x060036EE RID: 14062 RVA: 0x000D3A95 File Offset: 0x000D1C95
		public override void WriteData(TraceLoggingDataCollector collector, ref decimal value)
		{
			collector.AddScalar((double)value);
		}
	}
}
