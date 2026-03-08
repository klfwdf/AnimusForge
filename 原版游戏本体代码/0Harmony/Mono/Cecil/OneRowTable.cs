using System;

namespace Mono.Cecil
{
	// Token: 0x020001F6 RID: 502
	internal abstract class OneRowTable<TRow> : MetadataTable where TRow : struct
	{
		// Token: 0x1700024F RID: 591
		// (get) Token: 0x06000AAB RID: 2731 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public sealed override int Length
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x06000AAC RID: 2732 RVA: 0x0001B842 File Offset: 0x00019A42
		public sealed override void Sort()
		{
		}

		// Token: 0x0400034B RID: 843
		internal TRow row;
	}
}
