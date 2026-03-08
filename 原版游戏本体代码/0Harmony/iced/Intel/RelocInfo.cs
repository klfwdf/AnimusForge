using System;

namespace Iced.Intel
{
	// Token: 0x0200062A RID: 1578
	internal readonly struct RelocInfo
	{
		// Token: 0x060020EF RID: 8431 RVA: 0x000684DD File Offset: 0x000666DD
		public RelocInfo(RelocKind kind, ulong address)
		{
			this.Kind = kind;
			this.Address = address;
		}

		// Token: 0x0400167E RID: 5758
		public readonly ulong Address;

		// Token: 0x0400167F RID: 5759
		public readonly RelocKind Kind;
	}
}
