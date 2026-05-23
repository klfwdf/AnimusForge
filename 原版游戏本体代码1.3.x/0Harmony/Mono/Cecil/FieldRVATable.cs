using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x02000211 RID: 529
	internal sealed class FieldRVATable : SortedTable<Row<uint, uint>>
	{
		// Token: 0x06000AEF RID: 2799 RVA: 0x0002550C File Offset: 0x0002370C
		public override void Write(TableHeapBuffer buffer)
		{
			this.position = buffer.position;
			for (int i = 0; i < this.length; i++)
			{
				buffer.WriteUInt32(this.rows[i].Col1);
				buffer.WriteRID(this.rows[i].Col2, Table.Field);
			}
		}

		// Token: 0x06000AF0 RID: 2800 RVA: 0x00025175 File Offset: 0x00023375
		public override int Compare(Row<uint, uint> x, Row<uint, uint> y)
		{
			return SortedTable<Row<uint, uint>>.Compare(x.Col2, y.Col2);
		}

		// Token: 0x0400034E RID: 846
		internal int position;
	}
}
