using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007C2 RID: 1986
	internal sealed class OpCodeHandler_VEX_Gv_GPR_Ib : OpCodeHandlerModRM
	{
		// Token: 0x0600267A RID: 9850 RVA: 0x0008398A File Offset: 0x00081B8A
		public OpCodeHandler_VEX_Gv_GPR_Ib(Register baseReg, Code code32, Code code64)
		{
			this.baseReg = baseReg;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600267B RID: 9851 RVA: 0x000839A8 File Offset: 0x00081BA8
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
			}
			else
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040038D6 RID: 14550
		private readonly Register baseReg;

		// Token: 0x040038D7 RID: 14551
		private readonly Code code32;

		// Token: 0x040038D8 RID: 14552
		private readonly Code code64;
	}
}
