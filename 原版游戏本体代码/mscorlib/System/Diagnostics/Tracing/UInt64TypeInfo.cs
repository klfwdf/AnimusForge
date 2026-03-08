using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200045B RID: 1115
	internal sealed class UInt64TypeInfo : TraceLoggingTypeInfo<ulong>
	{
		// Token: 0x0600367E RID: 13950 RVA: 0x000D344E File Offset: 0x000D164E
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format64(format, TraceLoggingDataType.UInt64));
		}

		// Token: 0x0600367F RID: 13951 RVA: 0x000D345F File Offset: 0x000D165F
		public override void WriteData(TraceLoggingDataCollector collector, ref ulong value)
		{
			collector.AddScalar(value);
		}
	}
}
