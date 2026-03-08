using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x0200020F RID: 527
	internal sealed class TypeSpecTable : MetadataTable<uint>
	{
		// Token: 0x06000AEA RID: 2794 RVA: 0x00025448 File Offset: 0x00023648
		public override void Write(TableHeapBuffer buffer)
		{
			for (int i = 0; i < this.length; i++)
			{
				buffer.WriteBlob(this.rows[i]);
			}
		}
	}
}
