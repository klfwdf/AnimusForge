using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x020001F9 RID: 505
	internal sealed class ModuleTable : OneRowTable<Row<uint, uint>>
	{
		// Token: 0x06000AB7 RID: 2743 RVA: 0x00024B00 File Offset: 0x00022D00
		public override void Write(TableHeapBuffer buffer)
		{
			buffer.WriteUInt16(0);
			buffer.WriteString(this.row.Col1);
			buffer.WriteGuid(this.row.Col2);
			buffer.WriteUInt16(0);
			buffer.WriteUInt16(0);
		}
	}
}
