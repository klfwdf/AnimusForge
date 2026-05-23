using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200046D RID: 1133
	internal sealed class DoubleArrayTypeInfo : TraceLoggingTypeInfo<double[]>
	{
		// Token: 0x060036B4 RID: 14004 RVA: 0x000D373C File Offset: 0x000D193C
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format64(format, TraceLoggingDataType.Double));
		}

		// Token: 0x060036B5 RID: 14005 RVA: 0x000D374D File Offset: 0x000D194D
		public override void WriteData(TraceLoggingDataCollector collector, ref double[] value)
		{
			collector.AddArray(value);
		}
	}
}
