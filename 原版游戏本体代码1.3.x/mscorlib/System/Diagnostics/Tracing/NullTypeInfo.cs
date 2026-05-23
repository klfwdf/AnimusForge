using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000452 RID: 1106
	internal sealed class NullTypeInfo<DataType> : TraceLoggingTypeInfo<DataType>
	{
		// Token: 0x06003662 RID: 13922 RVA: 0x000D3322 File Offset: 0x000D1522
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddGroup(name);
		}

		// Token: 0x06003663 RID: 13923 RVA: 0x000D332C File Offset: 0x000D152C
		public override void WriteData(TraceLoggingDataCollector collector, ref DataType value)
		{
		}

		// Token: 0x06003664 RID: 13924 RVA: 0x000D332E File Offset: 0x000D152E
		public override object GetData(object value)
		{
			return null;
		}
	}
}
