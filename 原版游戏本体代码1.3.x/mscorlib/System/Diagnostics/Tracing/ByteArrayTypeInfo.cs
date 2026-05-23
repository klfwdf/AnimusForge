using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000462 RID: 1122
	internal sealed class ByteArrayTypeInfo : TraceLoggingTypeInfo<byte[]>
	{
		// Token: 0x06003693 RID: 13971 RVA: 0x000D3550 File Offset: 0x000D1750
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			switch (format)
			{
			case EventFieldFormat.String:
				collector.AddBinary(name, TraceLoggingDataType.CountedMbcsString);
				return;
			case EventFieldFormat.Boolean:
				collector.AddArray(name, TraceLoggingDataType.Boolean8);
				return;
			case EventFieldFormat.Hexadecimal:
				collector.AddArray(name, TraceLoggingDataType.HexInt8);
				return;
			default:
				if (format == EventFieldFormat.Xml)
				{
					collector.AddBinary(name, TraceLoggingDataType.CountedMbcsXml);
					return;
				}
				if (format != EventFieldFormat.Json)
				{
					collector.AddBinary(name, Statics.MakeDataType(TraceLoggingDataType.Binary, format));
					return;
				}
				collector.AddBinary(name, TraceLoggingDataType.CountedMbcsJson);
				return;
			}
		}

		// Token: 0x06003694 RID: 13972 RVA: 0x000D35C8 File Offset: 0x000D17C8
		public override void WriteData(TraceLoggingDataCollector collector, ref byte[] value)
		{
			collector.AddBinary(value);
		}
	}
}
