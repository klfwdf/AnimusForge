using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000464 RID: 1124
	internal sealed class Int16ArrayTypeInfo : TraceLoggingTypeInfo<short[]>
	{
		// Token: 0x06003699 RID: 13977 RVA: 0x000D35FC File Offset: 0x000D17FC
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format16(format, TraceLoggingDataType.Int16));
		}

		// Token: 0x0600369A RID: 13978 RVA: 0x000D360C File Offset: 0x000D180C
		public override void WriteData(TraceLoggingDataCollector collector, ref short[] value)
		{
			collector.AddArray(value);
		}
	}
}
