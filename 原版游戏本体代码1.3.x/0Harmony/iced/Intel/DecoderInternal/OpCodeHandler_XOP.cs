using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006F9 RID: 1785
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_XOP : OpCodeHandlerModRM
	{
		// Token: 0x060024D6 RID: 9430 RVA: 0x0007B0EC File Offset: 0x000792EC
		public OpCodeHandler_XOP(OpCodeHandler handler_reg0)
		{
			if (handler_reg0 == null)
			{
				throw new ArgumentNullException("handler_reg0");
			}
			this.handler_reg0 = handler_reg0;
		}

		// Token: 0x060024D7 RID: 9431 RVA: 0x0007B10A File Offset: 0x0007930A
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.modrm & 31U) < 8U)
			{
				this.handler_reg0.Decode(decoder, ref instruction);
				return;
			}
			decoder.XOP(ref instruction);
		}

		// Token: 0x0400376B RID: 14187
		private readonly OpCodeHandler handler_reg0;
	}
}
