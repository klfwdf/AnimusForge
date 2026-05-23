using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x02000217 RID: 535
	internal sealed class NestedClassTable : SortedTable<Row<uint, uint>>
	{
		// Token: 0x06000AFC RID: 2812 RVA: 0x000258A4 File Offset: 0x00023AA4
		public override void Write(TableHeapBuffer buffer)
		{
			for (int i = 0; i < this.length; i++)
			{
				buffer.WriteRID(this.rows[i].Col1, Table.TypeDef);
				buffer.WriteRID(this.rows[i].Col2, Table.TypeDef);
			}
		}

		// Token: 0x06000AFD RID: 2813 RVA: 0x0002500D File Offset: 0x0002320D
		public override int Compare(Row<uint, uint> x, Row<uint, uint> y)
		{
			return SortedTable<Row<uint, uint>>.Compare(x.Col1, y.Col1);
		}
	}
}
