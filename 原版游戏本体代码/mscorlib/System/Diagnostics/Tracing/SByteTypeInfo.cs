using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000455 RID: 1109
	internal sealed class SByteTypeInfo : TraceLoggingTypeInfo<sbyte>
	{
		// Token: 0x0600366C RID: 13932 RVA: 0x000D3381 File Offset: 0x000D1581
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format8(format, TraceLoggingDataType.Int8));
		}

		// Token: 0x0600366D RID: 13933 RVA: 0x000D3391 File Offset: 0x000D1591
		public override void WriteData(TraceLoggingDataCollector collector, ref sbyte value)
		{
			collector.AddScalar(value);
		}
	}
}
