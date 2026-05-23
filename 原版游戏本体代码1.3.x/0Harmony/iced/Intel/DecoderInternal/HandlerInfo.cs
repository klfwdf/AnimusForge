using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007D8 RID: 2008
	[NullableContext(2)]
	[Nullable(0)]
	internal readonly struct HandlerInfo
	{
		// Token: 0x060026A4 RID: 9892 RVA: 0x0008494F File Offset: 0x00082B4F
		[NullableContext(1)]
		public HandlerInfo(OpCodeHandler handler)
		{
			this.handler = handler;
			this.handlers = null;
		}

		// Token: 0x060026A5 RID: 9893 RVA: 0x0008495F File Offset: 0x00082B5F
		public HandlerInfo([Nullable(new byte[] { 1, 2 })] OpCodeHandler[] handlers)
		{
			this.handler = null;
			this.handlers = handlers;
		}

		// Token: 0x040038FE RID: 14590
		public readonly OpCodeHandler handler;

		// Token: 0x040038FF RID: 14591
		public readonly OpCodeHandler[] handlers;
	}
}
