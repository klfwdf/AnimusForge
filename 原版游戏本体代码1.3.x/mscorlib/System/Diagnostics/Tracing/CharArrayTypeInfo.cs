using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200046C RID: 1132
	internal sealed class CharArrayTypeInfo : TraceLoggingTypeInfo<char[]>
	{
		// Token: 0x060036B1 RID: 14001 RVA: 0x000D3716 File Offset: 0x000D1916
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format16(format, TraceLoggingDataType.Char16));
		}

		// Token: 0x060036B2 RID: 14002 RVA: 0x000D372A File Offset: 0x000D192A
		public override void WriteData(TraceLoggingDataCollector collector, ref char[] value)
		{
			collector.AddArray(value);
		}
	}
}
