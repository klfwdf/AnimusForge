using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200046A RID: 1130
	internal sealed class IntPtrArrayTypeInfo : TraceLoggingTypeInfo<IntPtr[]>
	{
		// Token: 0x060036AB RID: 13995 RVA: 0x000D36CA File Offset: 0x000D18CA
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.FormatPtr(format, Statics.IntPtrType));
		}

		// Token: 0x060036AC RID: 13996 RVA: 0x000D36DE File Offset: 0x000D18DE
		public override void WriteData(TraceLoggingDataCollector collector, ref IntPtr[] value)
		{
			collector.AddArray(value);
		}
	}
}
