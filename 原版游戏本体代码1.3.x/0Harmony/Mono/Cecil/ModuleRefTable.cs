using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x0200020E RID: 526
	internal sealed class ModuleRefTable : MetadataTable<uint>
	{
		// Token: 0x06000AE8 RID: 2792 RVA: 0x0002541C File Offset: 0x0002361C
		public override void Write(TableHeapBuffer buffer)
		{
			for (int i = 0; i < this.length; i++)
			{
				buffer.WriteString(this.rows[i]);
			}
		}
	}
}
