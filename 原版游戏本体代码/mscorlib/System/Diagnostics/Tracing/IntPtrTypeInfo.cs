using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200045C RID: 1116
	internal sealed class IntPtrTypeInfo : TraceLoggingTypeInfo<IntPtr>
	{
		// Token: 0x06003681 RID: 13953 RVA: 0x000D3471 File Offset: 0x000D1671
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.FormatPtr(format, Statics.IntPtrType));
		}

		// Token: 0x06003682 RID: 13954 RVA: 0x000D3485 File Offset: 0x000D1685
		public override void WriteData(TraceLoggingDataCollector collector, ref IntPtr value)
		{
			collector.AddScalar(value);
		}
	}
}
