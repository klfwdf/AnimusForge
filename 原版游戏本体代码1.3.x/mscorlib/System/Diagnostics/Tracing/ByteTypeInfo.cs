using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000454 RID: 1108
	internal sealed class ByteTypeInfo : TraceLoggingTypeInfo<byte>
	{
		// Token: 0x06003669 RID: 13929 RVA: 0x000D335F File Offset: 0x000D155F
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format8(format, TraceLoggingDataType.UInt8));
		}

		// Token: 0x0600366A RID: 13930 RVA: 0x000D336F File Offset: 0x000D156F
		public override void WriteData(TraceLoggingDataCollector collector, ref byte value)
		{
			collector.AddScalar(value);
		}
	}
}
