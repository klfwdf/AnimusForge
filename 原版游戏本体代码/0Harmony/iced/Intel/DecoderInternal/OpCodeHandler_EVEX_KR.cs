using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006D9 RID: 1753
	internal sealed class OpCodeHandler_EVEX_KR : OpCodeHandlerModRM
	{
		// Token: 0x06002494 RID: 9364 RVA: 0x000793B0 File Offset: 0x000775B0
		public OpCodeHandler_EVEX_KR(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x06002495 RID: 9365 RVA: 0x000793C8 File Offset: 0x000775C8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa | (StateFlags)decoder.state.zs.extraRegisterBase | (StateFlags)decoder.state.extraRegisterBaseEVEX) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.K0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x04003703 RID: 14083
		private readonly Register baseReg;

		// Token: 0x04003704 RID: 14084
		private readonly Code code;
	}
}
