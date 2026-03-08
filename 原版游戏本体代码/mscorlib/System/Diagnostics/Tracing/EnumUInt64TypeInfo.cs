using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000476 RID: 1142
	internal sealed class EnumUInt64TypeInfo<EnumType> : TraceLoggingTypeInfo<EnumType>
	{
		// Token: 0x060036D6 RID: 14038 RVA: 0x000D38C5 File Offset: 0x000D1AC5
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format64(format, TraceLoggingDataType.UInt64));
		}

		// Token: 0x060036D7 RID: 14039 RVA: 0x000D38D6 File Offset: 0x000D1AD6
		public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
		{
			collector.AddScalar(EnumHelper<ulong>.Cast<EnumType>(value));
		}

		// Token: 0x060036D8 RID: 14040 RVA: 0x000D38E9 File Offset: 0x000D1AE9
		public override object GetData(object value)
		{
			return value;
		}
	}
}
