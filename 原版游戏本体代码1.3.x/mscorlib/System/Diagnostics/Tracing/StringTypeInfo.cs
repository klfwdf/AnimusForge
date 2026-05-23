using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000477 RID: 1143
	internal sealed class StringTypeInfo : TraceLoggingTypeInfo<string>
	{
		// Token: 0x060036DA RID: 14042 RVA: 0x000D38F4 File Offset: 0x000D1AF4
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.AddBinary(name, Statics.MakeDataType(TraceLoggingDataType.CountedUtf16String, format));
		}

		// Token: 0x060036DB RID: 14043 RVA: 0x000D3905 File Offset: 0x000D1B05
		public override void WriteData(TraceLoggingDataCollector collector, ref string value)
		{
			collector.AddBinary(value);
		}

		// Token: 0x060036DC RID: 14044 RVA: 0x000D3910 File Offset: 0x000D1B10
		public override object GetData(object value)
		{
			object obj = base.GetData(value);
			if (obj == null)
			{
				obj = "";
			}
			return obj;
		}
	}
}
