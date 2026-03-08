using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000465 RID: 1125
	internal sealed class UInt16ArrayTypeInfo : TraceLoggingTypeInfo<ushort[]>
	{
		// Token: 0x0600369C RID: 13980 RVA: 0x000D361E File Offset: 0x000D181E
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddArray(name, Statics.Format16(format, TraceLoggingDataType.UInt16));
		}

		// Token: 0x0600369D RID: 13981 RVA: 0x000D362E File Offset: 0x000D182E
		public override void WriteData(TraceLoggingDataCollector collector, ref ushort[] value)
		{
			collector.AddArray(value);
		}
	}
}
