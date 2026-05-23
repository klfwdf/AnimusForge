using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006BA RID: 1722
	[NullableContext(1)]
	[Nullable(0)]
	internal readonly struct HandlerOptions
	{
		// Token: 0x0600244E RID: 9294 RVA: 0x0007739E File Offset: 0x0007559E
		public HandlerOptions(OpCodeHandler handler, DecoderOptions options)
		{
			this.handler = handler;
			this.options = options;
		}

		// Token: 0x04003682 RID: 13954
		public readonly OpCodeHandler handler;

		// Token: 0x04003683 RID: 13955
		public readonly DecoderOptions options;
	}
}
