using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006B8 RID: 1720
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Bitness_DontReadModRM : OpCodeHandlerModRM
	{
		// Token: 0x0600244A RID: 9290 RVA: 0x000772FF File Offset: 0x000754FF
		public OpCodeHandler_Bitness_DontReadModRM(OpCodeHandler handler1632, OpCodeHandler handler64)
		{
			this.handler1632 = handler1632;
			this.handler64 = handler64;
		}

		// Token: 0x0600244B RID: 9291 RVA: 0x00077318 File Offset: 0x00075518
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			OpCodeHandler handler;
			if (decoder.is64bMode)
			{
				handler = this.handler64;
			}
			else
			{
				handler = this.handler1632;
			}
			handler.Decode(decoder, ref instruction);
		}

		// Token: 0x0400367E RID: 13950
		private readonly OpCodeHandler handler1632;

		// Token: 0x0400367F RID: 13951
		private readonly OpCodeHandler handler64;
	}
}
