using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000772 RID: 1906
	internal sealed class OpCodeHandler_VW : OpCodeHandlerModRM
	{
		// Token: 0x060025D3 RID: 9683 RVA: 0x0007FFBA File Offset: 0x0007E1BA
		public OpCodeHandler_VW(Code codeR, Code codeM)
		{
			this.codeR = codeR;
			this.codeM = codeM;
		}

		// Token: 0x060025D4 RID: 9684 RVA: 0x0007FFD0 File Offset: 0x0007E1D0
		public OpCodeHandler_VW(Code code)
		{
			this.codeR = code;
			this.codeM = code;
		}

		// Token: 0x060025D5 RID: 9685 RVA: 0x0007FFE8 File Offset: 0x0007E1E8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				instruction.InternalSetCodeNoCheck(this.codeR);
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
				return;
			}
			instruction.InternalSetCodeNoCheck(this.codeM);
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003845 RID: 14405
		private readonly Code codeR;

		// Token: 0x04003846 RID: 14406
		private readonly Code codeM;
	}
}
