using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000472 RID: 1138
	internal sealed class EnumUInt16TypeInfo<EnumType> : TraceLoggingTypeInfo<EnumType>
	{
		// Token: 0x060036C6 RID: 14022 RVA: 0x000D380C File Offset: 0x000D1A0C
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format16(format, TraceLoggingDataType.UInt16));
		}

		// Token: 0x060036C7 RID: 14023 RVA: 0x000D381C File Offset: 0x000D1A1C
		public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
		{
			collector.AddScalar(EnumHelper<ushort>.Cast<EnumType>(value));
		}

		// Token: 0x060036C8 RID: 14024 RVA: 0x000D382F File Offset: 0x000D1A2F
		public override object GetData(object value)
		{
			return value;
		}
	}
}
