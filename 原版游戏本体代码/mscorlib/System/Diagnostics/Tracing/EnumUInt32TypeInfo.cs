using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000474 RID: 1140
	internal sealed class EnumUInt32TypeInfo<EnumType> : TraceLoggingTypeInfo<EnumType>
	{
		// Token: 0x060036CE RID: 14030 RVA: 0x000D3868 File Offset: 0x000D1A68
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format32(format, TraceLoggingDataType.UInt32));
		}

		// Token: 0x060036CF RID: 14031 RVA: 0x000D3878 File Offset: 0x000D1A78
		public override void WriteData(TraceLoggingDataCollector collector, ref EnumType value)
		{
			collector.AddScalar(EnumHelper<uint>.Cast<EnumType>(value));
		}

		// Token: 0x060036D0 RID: 14032 RVA: 0x000D388B File Offset: 0x000D1A8B
		public override object GetData(object value)
		{
			return value;
		}
	}
}
