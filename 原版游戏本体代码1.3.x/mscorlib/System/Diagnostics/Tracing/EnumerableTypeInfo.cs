using System;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200043F RID: 1087
	internal sealed class EnumerableTypeInfo<IterableType, ElementType> : TraceLoggingTypeInfo<IterableType> where IterableType : IEnumerable<ElementType>
	{
		// Token: 0x060035F4 RID: 13812 RVA: 0x000D2301 File Offset: 0x000D0501
		public EnumerableTypeInfo(TraceLoggingTypeInfo<ElementType> elementInfo)
		{
			this.elementInfo = elementInfo;
		}

		// Token: 0x060035F5 RID: 13813 RVA: 0x000D2310 File Offset: 0x000D0510
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.BeginBufferedArray();
			this.elementInfo.WriteMetadata(collector, name, format);
			collector.EndBufferedArray();
		}

		// Token: 0x060035F6 RID: 13814 RVA: 0x000D232C File Offset: 0x000D052C
		public override void WriteData(TraceLoggingDataCollector collector, ref IterableType value)
		{
			int bookmark = collector.BeginBufferedArray();
			int num = 0;
			if (value != null)
			{
				foreach (ElementType elementType in value)
				{
					ElementType elementType2 = elementType;
					this.elementInfo.WriteData(collector, ref elementType2);
					num++;
				}
			}
			collector.EndBufferedArray(bookmark, num);
		}

		// Token: 0x060035F7 RID: 13815 RVA: 0x000D23A8 File Offset: 0x000D05A8
		public override object GetData(object value)
		{
			IterableType iterableType = (IterableType)((object)value);
			List<object> list = new List<object>();
			foreach (ElementType elementType in iterableType)
			{
				list.Add(this.elementInfo.GetData(elementType));
			}
			return list.ToArray();
		}

		// Token: 0x0400181C RID: 6172
		private readonly TraceLoggingTypeInfo<ElementType> elementInfo;
	}
}
