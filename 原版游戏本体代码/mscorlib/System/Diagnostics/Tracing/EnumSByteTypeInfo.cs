using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000470 RID: 1136
	internal sealed class EnumSByteTypeInfo<EnumType> : TraceLoggingTypeInfo<EnumType>
	{
		// Token: 0x060036BE RID: 14014 RVA: 0x000D37B0 File Offset: 0x000D19B0
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format8(format, TraceLoggingDataType.Int8));
		}

		// Token: 0x060036BF RID: 14015 RVA: 0x000D37C0 File Offset: 0x000D19C0
		public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
		{
			collector.AddScalar(EnumHelper<sbyte>.Cast<EnumType>(value));
		}

		// Token: 0x060036C0 RID: 14016 RVA: 0x000D37D3 File Offset: 0x000D19D3
		public override object GetData(object value)
		{
			return value;
		}
	}
}
