using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006FA RID: 1786
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_EVEX : OpCodeHandlerModRM
	{
		// Token: 0x060024D8 RID: 9432 RVA: 0x0007B132 File Offset: 0x00079332
		public OpCodeHandler_EVEX(OpCodeHandler handlerMem)
		{
			if (handlerMem == null)
			{
				throw new ArgumentNullException("handlerMem");
			}
			this.handlerMem = handlerMem;
		}

		// Token: 0x060024D9 RID: 9433 RVA: 0x0007B150 File Offset: 0x00079350
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				decoder.EVEX_MVEX(ref instruction);
				return;
			}
			if (decoder.state.mod == 3U)
			{
				decoder.EVEX_MVEX(ref instruction);
				return;
			}
			this.handlerMem.Decode(decoder, ref instruction);
		}

		// Token: 0x0400376C RID: 14188
		private readonly OpCodeHandler handlerMem;
	}
}
