using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006F7 RID: 1783
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_VEX2 : OpCodeHandlerModRM
	{
		// Token: 0x060024D2 RID: 9426 RVA: 0x0007B046 File Offset: 0x00079246
		public OpCodeHandler_VEX2(OpCodeHandler handlerMem)
		{
			if (handlerMem == null)
			{
				throw new ArgumentNullException("handlerMem");
			}
			this.handlerMem = handlerMem;
		}

		// Token: 0x060024D3 RID: 9427 RVA: 0x0007B064 File Offset: 0x00079264
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				decoder.VEX2(ref instruction);
				return;
			}
			if (decoder.state.mod == 3U)
			{
				decoder.VEX2(ref instruction);
				return;
			}
			this.handlerMem.Decode(decoder, ref instruction);
		}

		// Token: 0x04003769 RID: 14185
		private readonly OpCodeHandler handlerMem;
	}
}
