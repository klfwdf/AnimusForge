using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000721 RID: 1825
	internal sealed class OpCodeHandler_Gd_Rd : OpCodeHandlerModRM
	{
		// Token: 0x06002529 RID: 9513 RVA: 0x0007C69F File Offset: 0x0007A89F
		public OpCodeHandler_Gd_Rd(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600252A RID: 9514 RVA: 0x0007C6B0 File Offset: 0x0007A8B0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.EAX;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.EAX;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x040037A8 RID: 14248
		private readonly Code code;
	}
}
