using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000460 RID: 1120
	internal sealed class CharTypeInfo : TraceLoggingTypeInfo<char>
	{
		// Token: 0x0600368D RID: 13965 RVA: 0x000D3503 File Offset: 0x000D1703
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format16(format, TraceLoggingDataType.Char16));
		}

		// Token: 0x0600368E RID: 13966 RVA: 0x000D3517 File Offset: 0x000D1717
		public override void WriteData(TraceLoggingDataCollector collector, ref char value)
		{
			collector.AddScalar(value);
		}
	}
}
