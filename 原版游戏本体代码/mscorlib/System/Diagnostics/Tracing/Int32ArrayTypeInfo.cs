using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000466 RID: 1126
	internal sealed class Int32ArrayTypeInfo : TraceLoggingTypeInfo<int[]>
	{
		// Token: 0x0600369F RID: 13983 RVA: 0x000D3640 File Offset: 0x000D1840
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format32(format, TraceLoggingDataType.Int32));
		}

		// Token: 0x060036A0 RID: 13984 RVA: 0x000D3650 File Offset: 0x000D1850
		public override void WriteData(TraceLoggingDataCollector collector, ref int[] value)
		{
			collector.AddArray(value);
		}
	}
}
