using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200047B RID: 1147
	internal sealed class DateTimeOffsetTypeInfo : TraceLoggingTypeInfo<DateTimeOffset>
	{
		// Token: 0x060036E7 RID: 14055 RVA: 0x000D39D4 File Offset: 0x000D1BD4
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			TraceLoggingMetadataCollector traceLoggingMetadataCollector = collector.AddGroup(name);
			traceLoggingMetadataCollector.AddScalar("Ticks", Statics.MakeDataType(TraceLoggingDataType.FileTime, format));
			traceLoggingMetadataCollector.AddScalar("Offset", TraceLoggingDataType.Int64);
		}

		// Token: 0x060036E8 RID: 14056 RVA: 0x000D3A0C File Offset: 0x000D1C0C
		public override void WriteData(TraceLoggingDataCollector collector, ref DateTimeOffset value)
		{
			long ticks = value.Ticks;
			collector.AddScalar((ticks < 504911232000000000L) ? 0L : (ticks - 504911232000000000L));
			collector.AddScalar(value.Offset.Ticks);
		}
	}
}
