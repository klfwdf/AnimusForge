using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006B6 RID: 1718
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_W : OpCodeHandlerModRM
	{
		// Token: 0x06002446 RID: 9286 RVA: 0x00077248 File Offset: 0x00075448
		public OpCodeHandler_W(OpCodeHandler handlerW0, OpCodeHandler handlerW1)
		{
			if (handlerW0 == null)
			{
				throw new ArgumentNullException("handlerW0");
			}
			this.handlerW0 = handlerW0;
			if (handlerW1 == null)
			{
				throw new ArgumentNullException("handlerW1");
			}
			this.handlerW1 = handlerW1;
		}

		// Token: 0x06002447 RID: 9287 RVA: 0x0007727C File Offset: 0x0007547C
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			(((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U) ? this.handlerW1 : this.handlerW0).Decode(decoder, ref instruction);
		}

		// Token: 0x0400367A RID: 13946
		private readonly OpCodeHandler handlerW0;

		// Token: 0x0400367B RID: 13947
		private readonly OpCodeHandler handlerW1;
	}
}
