using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007C1 RID: 1985
	internal sealed class OpCodeHandler_VEX_Gv_RX : OpCodeHandlerModRM
	{
		// Token: 0x06002678 RID: 9848 RVA: 0x000838A8 File Offset: 0x00081AA8
		public OpCodeHandler_VEX_Gv_RX(Register baseReg, Code code32, Code code64)
		{
			this.baseReg = baseReg;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002679 RID: 9849 RVA: 0x000838C8 File Offset: 0x00081AC8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
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
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + gpr;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x040038D3 RID: 14547
		private readonly Register baseReg;

		// Token: 0x040038D4 RID: 14548
		private readonly Code code32;

		// Token: 0x040038D5 RID: 14549
		private readonly Code code64;
	}
}
