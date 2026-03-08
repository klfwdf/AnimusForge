using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000467 RID: 1127
	internal sealed class UInt32ArrayTypeInfo : TraceLoggingTypeInfo<uint[]>
	{
		// Token: 0x060036A2 RID: 13986 RVA: 0x000D3662 File Offset: 0x000D1862
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format32(format, TraceLoggingDataType.UInt32));
		}

		// Token: 0x060036A3 RID: 13987 RVA: 0x000D3672 File Offset: 0x000D1872
		public override void WriteData(TraceLoggingDataCollector collector, ref uint[] value)
		{
			collector.AddArray(value);
		}
	}
}
