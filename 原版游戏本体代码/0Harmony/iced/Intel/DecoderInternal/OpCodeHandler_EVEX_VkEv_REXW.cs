using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006EB RID: 1771
	internal sealed class OpCodeHandler_EVEX_VkEv_REXW : OpCodeHandlerModRM
	{
		// Token: 0x060024BB RID: 9403 RVA: 0x0007A896 File Offset: 0x00078A96
		public OpCodeHandler_EVEX_VkEv_REXW(Register baseReg, Code code32)
		{
			this.baseReg = baseReg;
			this.code32 = code32;
			this.code64 = Code.INVALID;
		}

		// Token: 0x060024BC RID: 9404 RVA: 0x0007A8B3 File Offset: 0x00078AB3
		public OpCodeHandler_EVEX_VkEv_REXW(Register baseReg, Code code32, Code code64)
		{
			this.baseReg = baseReg;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x060024BD RID: 9405 RVA: 0x0007A8D0 File Offset: 0x00078AD0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & StateFlags.b) | (StateFlags)decoder.state.vvvv_invalidCheck) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			Register gpr;
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				gpr = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				gpr = Register.EAX;
			}
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x0400374B RID: 14155
		private readonly Register baseReg;

		// Token: 0x0400374C RID: 14156
		private readonly Code code32;

		// Token: 0x0400374D RID: 14157
		private readonly Code code64;
	}
}
