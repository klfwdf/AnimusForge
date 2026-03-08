using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000468 RID: 1128
	internal sealed class Int64ArrayTypeInfo : TraceLoggingTypeInfo<long[]>
	{
		// Token: 0x060036A5 RID: 13989 RVA: 0x000D3684 File Offset: 0x000D1884
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format64(format, TraceLoggingDataType.Int64));
		}

		// Token: 0x060036A6 RID: 13990 RVA: 0x000D3695 File Offset: 0x000D1895
		public override void WriteData(TraceLoggingDataCollector collector, ref long[] value)
		{
			collector.AddArray(value);
		}
	}
}
