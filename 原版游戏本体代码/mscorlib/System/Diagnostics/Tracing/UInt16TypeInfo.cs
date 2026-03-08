using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000457 RID: 1111
	internal sealed class UInt16TypeInfo : TraceLoggingTypeInfo<ushort>
	{
		// Token: 0x06003672 RID: 13938 RVA: 0x000D33C5 File Offset: 0x000D15C5
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format16(format, TraceLoggingDataType.UInt16));
		}

		// Token: 0x06003673 RID: 13939 RVA: 0x000D33D5 File Offset: 0x000D15D5
		public override void WriteData(TraceLoggingDataCollector collector, ref ushort value)
		{
			collector.AddScalar(value);
		}
	}
}
