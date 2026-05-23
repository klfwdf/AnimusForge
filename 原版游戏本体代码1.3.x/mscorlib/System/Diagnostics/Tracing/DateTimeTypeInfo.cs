using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200047A RID: 1146
	internal sealed class DateTimeTypeInfo : TraceLoggingTypeInfo<DateTime>
	{
		// Token: 0x060036E4 RID: 14052 RVA: 0x000D3981 File Offset: 0x000D1B81
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.FileTime, format));
		}

		// Token: 0x060036E5 RID: 14053 RVA: 0x000D3994 File Offset: 0x000D1B94
		public override void WriteData(TraceLoggingDataCollector collector, ref DateTime value)
		{
			long ticks = value.Ticks;
			collector.AddScalar((ticks < 504911232000000000L) ? 0L : (ticks - 504911232000000000L));
		}
	}
}
