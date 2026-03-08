using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007A7 RID: 1959
	internal sealed class OpCodeHandler_VEX_MV : OpCodeHandlerModRM
	{
		// Token: 0x06002640 RID: 9792 RVA: 0x000824A9 File Offset: 0x000806A9
		public OpCodeHandler_VEX_MV(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x06002641 RID: 9793 RVA: 0x000824C0 File Offset: 0x000806C0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400389B RID: 14491
		private readonly Register baseReg;

		// Token: 0x0400389C RID: 14492
		private readonly Code code;
	}
}
