using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000459 RID: 1113
	internal sealed class UInt32TypeInfo : TraceLoggingTypeInfo<uint>
	{
		// Token: 0x06003678 RID: 13944 RVA: 0x000D3409 File Offset: 0x000D1609
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format32(format, TraceLoggingDataType.UInt32));
		}

		// Token: 0x06003679 RID: 13945 RVA: 0x000D3419 File Offset: 0x000D1619
		public override void WriteData(TraceLoggingDataCollector collector, ref uint value)
		{
			collector.AddScalar(value);
		}
	}
}
