using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200046E RID: 1134
	internal sealed class SingleArrayTypeInfo : TraceLoggingTypeInfo<float[]>
	{
		// Token: 0x060036B7 RID: 14007 RVA: 0x000D375F File Offset: 0x000D195F
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format32(format, TraceLoggingDataType.Float));
		}

		// Token: 0x060036B8 RID: 14008 RVA: 0x000D3770 File Offset: 0x000D1970
		public override void WriteData(TraceLoggingDataCollector collector, ref float[] value)
		{
			collector.AddArray(value);
		}
	}
}
