using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000463 RID: 1123
	internal sealed class SByteArrayTypeInfo : TraceLoggingTypeInfo<sbyte[]>
	{
		// Token: 0x06003696 RID: 13974 RVA: 0x000D35DA File Offset: 0x000D17DA
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format8(format, TraceLoggingDataType.Int8));
		}

		// Token: 0x06003697 RID: 13975 RVA: 0x000D35EA File Offset: 0x000D17EA
		public override void WriteData(TraceLoggingDataCollector collector, ref sbyte[] value)
		{
			collector.AddArray(value);
		}
	}
}
