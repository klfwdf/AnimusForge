using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006F8 RID: 1784
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_VEX3 : OpCodeHandlerModRM
	{
		// Token: 0x060024D4 RID: 9428 RVA: 0x0007B099 File Offset: 0x00079299
		public OpCodeHandler_VEX3(OpCodeHandler handlerMem)
		{
			if (handlerMem == null)
			{
				throw new ArgumentNullException("handlerMem");
			}
			this.handlerMem = handlerMem;
		}

		// Token: 0x060024D5 RID: 9429 RVA: 0x0007B0B7 File Offset: 0x000792B7
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				decoder.VEX3(ref instruction);
				return;
			}
			if (decoder.state.mod == 3U)
			{
				decoder.VEX3(ref instruction);
				return;
			}
			this.handlerMem.Decode(decoder, ref instruction);
		}

		// Token: 0x0400376A RID: 14186
		private readonly OpCodeHandler handlerMem;
	}
}
