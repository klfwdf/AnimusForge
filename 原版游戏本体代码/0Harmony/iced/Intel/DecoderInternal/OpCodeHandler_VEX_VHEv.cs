using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007A0 RID: 1952
	internal sealed class OpCodeHandler_VEX_VHEv : OpCodeHandlerModRM
	{
		// Token: 0x06002631 RID: 9777 RVA: 0x00081ECB File Offset: 0x000800CB
		public OpCodeHandler_VEX_VHEv(Register baseReg, Code codeW0, Code codeW1)
		{
			this.baseReg = baseReg;
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
		}

		// Token: 0x06002632 RID: 9778 RVA: 0x00081EE8 File Offset: 0x000800E8
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
				return;
			}
			instruction.Op2Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003889 RID: 14473
		private readonly Register baseReg;

		// Token: 0x0400388A RID: 14474
		private readonly Code codeW0;

		// Token: 0x0400388B RID: 14475
		private readonly Code codeW1;
	}
}
