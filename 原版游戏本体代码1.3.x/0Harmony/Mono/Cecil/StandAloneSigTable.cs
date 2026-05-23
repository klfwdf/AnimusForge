using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x02000207 RID: 519
	internal sealed class StandAloneSigTable : MetadataTable<uint>
	{
		// Token: 0x06000AD9 RID: 2777 RVA: 0x00025188 File Offset: 0x00023388
		public override void Write(TableHeapBuffer buffer)
		{
			for (int i = 0; i < this.length; i++)
			{
				buffer.WriteBlob(this.rows[i]);
			}
		}
	}
}
