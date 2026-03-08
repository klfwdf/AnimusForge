using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000479 RID: 1145
	internal sealed class GuidArrayTypeInfo : TraceLoggingTypeInfo<Guid[]>
	{
		// Token: 0x060036E1 RID: 14049 RVA: 0x000D395E File Offset: 0x000D1B5E
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.MakeDataType(TraceLoggingDataType.Guid, format));
		}

		// Token: 0x060036E2 RID: 14050 RVA: 0x000D396F File Offset: 0x000D1B6F
		public override void WriteData(TraceLoggingDataCollector collector, ref Guid[] value)
		{
			collector.AddArray(value);
		}
	}
}
