using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x020001F5 RID: 501
	internal abstract class MetadataTable
	{
		// Token: 0x1700024D RID: 589
		// (get) Token: 0x06000AA6 RID: 2726
		public abstract int Length { get; }

		// Token: 0x1700024E RID: 590
		// (get) Token: 0x06000AA7 RID: 2727 RVA: 0x00024A1E File Offset: 0x00022C1E
		public bool IsLarge
		{
			get
			{
				return this.Length > 65535;
			}
		}

		// Token: 0x06000AA8 RID: 2728
		public abstract void Write(TableHeapBuffer buffer);

		// Token: 0x06000AA9 RID: 2729
		public abstract void Sort();
	}
}
