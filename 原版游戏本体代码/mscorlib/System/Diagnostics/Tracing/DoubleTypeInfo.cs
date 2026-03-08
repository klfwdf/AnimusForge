using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200045E RID: 1118
	internal sealed class DoubleTypeInfo : TraceLoggingTypeInfo<double>
	{
		// Token: 0x06003687 RID: 13959 RVA: 0x000D34BD File Offset: 0x000D16BD
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format64(format, TraceLoggingDataType.Double));
		}

		// Token: 0x06003688 RID: 13960 RVA: 0x000D34CE File Offset: 0x000D16CE
		public override void WriteData(TraceLoggingDataCollector collector, ref double value)
		{
			collector.AddScalar(value);
		}
	}
}
