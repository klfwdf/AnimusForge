using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200045A RID: 1114
	internal sealed class Int64TypeInfo : TraceLoggingTypeInfo<long>
	{
		// Token: 0x0600367B RID: 13947 RVA: 0x000D342B File Offset: 0x000D162B
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddScalar(name, Statics.Format64(format, TraceLoggingDataType.Int64));
		}

		// Token: 0x0600367C RID: 13948 RVA: 0x000D343C File Offset: 0x000D163C
		public override void WriteData(TraceLoggingDataCollector collector, ref long value)
		{
			collector.AddScalar(value);
		}
	}
}
