using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200045D RID: 1117
	internal sealed class UIntPtrTypeInfo : TraceLoggingTypeInfo<UIntPtr>
	{
		// Token: 0x06003684 RID: 13956 RVA: 0x000D3497 File Offset: 0x000D1697
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.FormatPtr(format, Statics.UIntPtrType));
		}

		// Token: 0x06003685 RID: 13957 RVA: 0x000D34AB File Offset: 0x000D16AB
		public override void WriteData(TraceLoggingDataCollector collector, ref UIntPtr value)
		{
			collector.AddScalar(value);
		}
	}
}
