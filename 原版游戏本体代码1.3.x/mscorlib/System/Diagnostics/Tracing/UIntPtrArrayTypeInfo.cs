using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200046B RID: 1131
	internal sealed class UIntPtrArrayTypeInfo : TraceLoggingTypeInfo<UIntPtr[]>
	{
		// Token: 0x060036AE RID: 13998 RVA: 0x000D36F0 File Offset: 0x000D18F0
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.FormatPtr(format, Statics.UIntPtrType));
		}

		// Token: 0x060036AF RID: 13999 RVA: 0x000D3704 File Offset: 0x000D1904
		public override void WriteData(TraceLoggingDataCollector collector, ref UIntPtr[] value)
		{
			collector.AddArray(value);
		}
	}
}
