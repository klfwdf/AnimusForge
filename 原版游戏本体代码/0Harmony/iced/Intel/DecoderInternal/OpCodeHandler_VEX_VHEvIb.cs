using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007A1 RID: 1953
	internal sealed class OpCodeHandler_VEX_VHEvIb : OpCodeHandlerModRM
	{
		// Token: 0x06002633 RID: 9779 RVA: 0x00081FB2 File Offset: 0x000801B2
		public OpCodeHandler_VEX_VHEvIb(Register baseReg, Code codeW0, Code codeW1)
		{
			this.baseReg = baseReg;
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
		}

		// Token: 0x06002634 RID: 9780 RVA: 0x00081FD0 File Offset: 0x000801D0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			Register gpr;
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.codeW1);
				gpr = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.codeW0);
				gpr = Register.EAX;
			}
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
			}
			else
			{
				instruction.Op2Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			instruction.Op3Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x0400388C RID: 14476
		private readonly Register baseReg;

		// Token: 0x0400388D RID: 14477
		private readonly Code codeW0;

		// Token: 0x0400388E RID: 14478
		private readonly Code codeW1;
	}
}
