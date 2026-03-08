using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x02000208 RID: 520
	internal sealed class EventMapTable : MetadataTable<Row<uint, uint>>
	{
		// Token: 0x06000ADB RID: 2779 RVA: 0x000251BC File Offset: 0x000233BC
		public override void Write(TableHeapBuffer buffer)
		{
			for (int i = 0; i < this.length; i++)
			{
				buffer.WriteRID(this.rows[i].Col1, Table.TypeDef);
				buffer.WriteRID(this.rows[i].Col2, Table.Event);
			}
		}
	}
}
