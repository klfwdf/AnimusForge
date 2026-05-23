using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000783 RID: 1923
	internal sealed class OpCodeHandler_V_Ev : OpCodeHandlerModRM
	{
		// Token: 0x060025F6 RID: 9718 RVA: 0x000809D9 File Offset: 0x0007EBD9
		public OpCodeHandler_V_Ev(Code codeW0, Code codeW1)
		{
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
		}

		// Token: 0x060025F7 RID: 9719 RVA: 0x000809F0 File Offset: 0x0007EBF0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			Register gpr;
			if (decoder.state.operandSize != OpSize.Size64)
			{
				instruction.InternalSetCodeNoCheck(this.codeW0);
				gpr = Register.EAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.codeW1);
				gpr = Register.RAX;
			}
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400385B RID: 14427
		private readonly Code codeW0;

		// Token: 0x0400385C RID: 14428
		private readonly Code codeW1;
	}
}
