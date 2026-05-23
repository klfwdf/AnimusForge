using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x0200021D RID: 541
	internal sealed class LocalScopeTable : MetadataTable<Row<uint, uint, uint, uint, uint, uint>>
	{
		// Token: 0x06000B09 RID: 2825 RVA: 0x00025AEC File Offset: 0x00023CEC
		public override void Write(TableHeapBuffer buffer)
		{
			for (int i = 0; i < this.length; i++)
			{
				buffer.WriteRID(this.rows[i].Col1, Table.Method);
				buffer.WriteRID(this.rows[i].Col2, Table.ImportScope);
				buffer.WriteRID(this.rows[i].Col3, Table.LocalVariable);
				buffer.WriteRID(this.rows[i].Col4, Table.LocalConstant);
				buffer.WriteUInt32(this.rows[i].Col5);
				buffer.WriteUInt32(this.rows[i].Col6);
			}
		}
	}
}
