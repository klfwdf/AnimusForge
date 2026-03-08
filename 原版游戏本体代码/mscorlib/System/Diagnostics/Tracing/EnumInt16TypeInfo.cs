using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000471 RID: 1137
	internal sealed class EnumInt16TypeInfo<EnumType> : TraceLoggingTypeInfo<EnumType>
	{
		// Token: 0x060036C2 RID: 14018 RVA: 0x000D37DE File Offset: 0x000D19DE
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format16(format, TraceLoggingDataType.Int16));
		}

		// Token: 0x060036C3 RID: 14019 RVA: 0x000D37EE File Offset: 0x000D19EE
		public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
		{
			collector.AddScalar(EnumHelper<short>.Cast<EnumType>(value));
		}

		// Token: 0x060036C4 RID: 14020 RVA: 0x000D3801 File Offset: 0x000D1A01
		public override object GetData(object value)
		{
			return value;
		}
	}
}
