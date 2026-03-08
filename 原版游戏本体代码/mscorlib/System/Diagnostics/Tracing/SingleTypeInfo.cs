using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200045F RID: 1119
	internal sealed class SingleTypeInfo : TraceLoggingTypeInfo<float>
	{
		// Token: 0x0600368A RID: 13962 RVA: 0x000D34E0 File Offset: 0x000D16E0
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format32(format, TraceLoggingDataType.Float));
		}

		// Token: 0x0600368B RID: 13963 RVA: 0x000D34F1 File Offset: 0x000D16F1
		public override void WriteData(TraceLoggingDataCollector collector, ref float value)
		{
			collector.AddScalar(value);
		}
	}
}
