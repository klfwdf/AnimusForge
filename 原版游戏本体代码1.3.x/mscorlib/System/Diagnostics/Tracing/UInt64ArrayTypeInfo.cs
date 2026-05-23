using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000469 RID: 1129
	internal sealed class UInt64ArrayTypeInfo : TraceLoggingTypeInfo<ulong[]>
	{
		// Token: 0x060036A8 RID: 13992 RVA: 0x000D36A7 File Offset: 0x000D18A7
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format64(format, TraceLoggingDataType.UInt64));
		}

		// Token: 0x060036A9 RID: 13993 RVA: 0x000D36B8 File Offset: 0x000D18B8
		public override void WriteData(TraceLoggingDataCollector collector, ref ulong[] value)
		{
			collector.AddArray(value);
		}
	}
}
