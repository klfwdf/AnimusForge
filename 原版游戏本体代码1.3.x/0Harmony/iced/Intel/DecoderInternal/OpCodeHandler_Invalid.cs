using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006AC RID: 1708
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Invalid : OpCodeHandlerModRM
	{
		// Token: 0x0600242F RID: 9263 RVA: 0x00076F25 File Offset: 0x00075125
		private OpCodeHandler_Invalid()
		{
		}

		// Token: 0x06002430 RID: 9264 RVA: 0x00076F2D File Offset: 0x0007512D
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.SetInvalidInstruction();
		}

		// Token: 0x0400366E RID: 13934
		public static readonly OpCodeHandler_Invalid Instance = new OpCodeHandler_Invalid();
	}
}
