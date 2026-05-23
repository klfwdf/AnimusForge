using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000473 RID: 1139
	internal sealed class EnumInt32TypeInfo<EnumType> : TraceLoggingTypeInfo<EnumType>
	{
		// Token: 0x060036CA RID: 14026 RVA: 0x000D383A File Offset: 0x000D1A3A
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format32(format, TraceLoggingDataType.Int32));
		}

		// Token: 0x060036CB RID: 14027 RVA: 0x000D384A File Offset: 0x000D1A4A
		public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
		{
			collector.AddScalar(EnumHelper<int>.Cast<EnumType>(value));
		}

		// Token: 0x060036CC RID: 14028 RVA: 0x000D385D File Offset: 0x000D1A5D
		public override object GetData(object value)
		{
			return value;
		}
	}
}
