using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007C0 RID: 1984
	internal sealed class OpCodeHandler_VEX_Gv_W : OpCodeHandlerModRM
	{
		// Token: 0x06002676 RID: 9846 RVA: 0x0008379C File Offset: 0x0008199C
		public OpCodeHandler_VEX_Gv_W(Register baseReg, Code codeW0, Code codeW1)
		{
			this.baseReg = baseReg;
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
		}

		// Token: 0x06002677 RID: 9847 RVA: 0x000837BC File Offset: 0x000819BC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.codeW1);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.codeW0);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.EAX;
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038D0 RID: 14544
		private readonly Register baseReg;

		// Token: 0x040038D1 RID: 14545
		private readonly Code codeW0;

		// Token: 0x040038D2 RID: 14546
		private readonly Code codeW1;
	}
}
