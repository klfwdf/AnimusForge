using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200043A RID: 1082
	internal sealed class ArrayTypeInfo<ElementType> : TraceLoggingTypeInfo<ElementType[]>
	{
		// Token: 0x060035DA RID: 13786 RVA: 0x000D1C9F File Offset: 0x000CFE9F
		public ArrayTypeInfo(TraceLoggingTypeInfo<ElementType> elementInfo)
		{
			this.elementInfo = elementInfo;
		}

		// Token: 0x060035DB RID: 13787 RVA: 0x000D1CAE File Offset: 0x000CFEAE
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			collector.BeginBufferedArray();
			this.elementInfo.WriteMetadata(collector, name, format);
			collector.EndBufferedArray();
		}

		// Token: 0x060035DC RID: 13788 RVA: 0x000D1CCC File Offset: 0x000CFECC
		public override void WriteData(TraceLoggingDataCollector collector, ref ElementType[] value)
		{
			int bookmark = collector.BeginBufferedArray();
			int count = 0;
			if (value != null)
			{
				count = value.Length;
				for (int i = 0; i < value.Length; i++)
				{
					this.elementInfo.WriteData(collector, ref value[i]);
				}
			}
			collector.EndBufferedArray(bookmark, count);
		}

		// Token: 0x060035DD RID: 13789 RVA: 0x000D1D18 File Offset: 0x000CFF18
		public override object GetData(object value)
		{
			ElementType[] array = (ElementType[])value;
			object[] array2 = new object[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = this.elementInfo.GetData(array[i]);
			}
			return array2;
		}

		// Token: 0x0400180E RID: 6158
		private readonly TraceLoggingTypeInfo<ElementType> elementInfo;
	}
}
