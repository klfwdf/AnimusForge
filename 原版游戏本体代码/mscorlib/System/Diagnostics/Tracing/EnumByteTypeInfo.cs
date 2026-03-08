using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200046F RID: 1135
	internal sealed class EnumByteTypeInfo<EnumType> : TraceLoggingTypeInfo<EnumType>
	{
		// Token: 0x060036BA RID: 14010 RVA: 0x000D3782 File Offset: 0x000D1982
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format8(format, TraceLoggingDataType.UInt8));
		}

		// Token: 0x060036BB RID: 14011 RVA: 0x000D3792 File Offset: 0x000D1992
		public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
		{
			collector.AddScalar(EnumHelper<byte>.Cast<EnumType>(value));
		}

		// Token: 0x060036BC RID: 14012 RVA: 0x000D37A5 File Offset: 0x000D19A5
		public override object GetData(object value)
		{
			return value;
		}
	}
}
