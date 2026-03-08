using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000475 RID: 1141
	internal sealed class EnumInt64TypeInfo<EnumType> : TraceLoggingTypeInfo<EnumType>
	{
		// Token: 0x060036D2 RID: 14034 RVA: 0x000D3896 File Offset: 0x000D1A96
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format64(format, TraceLoggingDataType.Int64));
		}

		// Token: 0x060036D3 RID: 14035 RVA: 0x000D38A7 File Offset: 0x000D1AA7
		public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
		{
			collector.AddScalar(EnumHelper<long>.Cast<EnumType>(value));
		}

		// Token: 0x060036D4 RID: 14036 RVA: 0x000D38BA File Offset: 0x000D1ABA
		public override object GetData(object value)
		{
			return value;
		}
	}
}
