using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200047C RID: 1148
	internal sealed class TimeSpanTypeInfo : TraceLoggingTypeInfo<TimeSpan>
	{
		// Token: 0x060036EA RID: 14058 RVA: 0x000D3A5D File Offset: 0x000D1C5D
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.Int64, format));
		}

		// Token: 0x060036EB RID: 14059 RVA: 0x000D3A6E File Offset: 0x000D1C6E
		public override void WriteData(TraceLoggingDataCollector collector, ref TimeSpan value)
		{
			collector.AddScalar(value.Ticks);
		}
	}
}
