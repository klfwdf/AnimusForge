using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007D0 RID: 2000
	internal sealed class OpCodeHandler_VEX_Gq_HK_RK : OpCodeHandlerModRM
	{
		// Token: 0x06002696 RID: 9878 RVA: 0x0008450A File Offset: 0x0008270A
		public OpCodeHandler_VEX_Gq_HK_RK(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002697 RID: 9879 RVA: 0x0008451C File Offset: 0x0008271C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.invalidCheckMask != 0U && decoder.state.vvvv > 7U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.RAX;
			instruction.Op1Register = (int)(decoder.state.vvvv & 7U) + Register.K0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)decoder.state.rm + Register.K0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x040038F2 RID: 14578
		private readonly Code code;
	}
}
