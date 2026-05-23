using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000782 RID: 1922
	internal sealed class OpCodeHandler_Gv_W : OpCodeHandlerModRM
	{
		// Token: 0x060025F4 RID: 9716 RVA: 0x000808F3 File Offset: 0x0007EAF3
		public OpCodeHandler_Gv_W(Code codeW0, Code codeW1)
		{
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
		}

		// Token: 0x060025F5 RID: 9717 RVA: 0x0008090C File Offset: 0x0007EB0C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
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
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003859 RID: 14425
		private readonly Code codeW0;

		// Token: 0x0400385A RID: 14426
		private readonly Code codeW1;
	}
}
