using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000478 RID: 1144
	internal sealed class GuidTypeInfo : TraceLoggingTypeInfo<Guid>
	{
		// Token: 0x060036DE RID: 14046 RVA: 0x000D3937 File Offset: 0x000D1B37
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.MakeDataType(TraceLoggingDataType.Guid, format));
		}

		// Token: 0x060036DF RID: 14047 RVA: 0x000D3948 File Offset: 0x000D1B48
		public override void WriteData(TraceLoggingDataCollector collector, ref Guid value)
		{
			collector.AddScalar(value);
		}
	}
}
