using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x02000222 RID: 546
	internal sealed class CustomDebugInformationTable : SortedTable<Row<uint, uint, uint>>
	{
		// Token: 0x06000B13 RID: 2835 RVA: 0x00025D04 File Offset: 0x00023F04
		public override void Write(TableHeapBuffer buffer)
		{
			for (int i = 0; i < this.length; i++)
			{
				buffer.WriteCodedRID(this.rows[i].Col1, CodedIndex.HasCustomDebugInformation);
				buffer.WriteGuid(this.rows[i].Col2);
				buffer.WriteBlob(this.rows[i].Col3);
			}
		}

		// Token: 0x06000B14 RID: 2836 RVA: 0x00024FA2 File Offset: 0x000231A2
		public override int Compare(Row<uint, uint, uint> x, Row<uint, uint, uint> y)
		{
			return SortedTable<Row<uint, uint, uint>>.Compare(x.Col1, y.Col1);
		}
	}
}
