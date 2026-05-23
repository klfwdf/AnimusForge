using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200077D RID: 1917
	internal sealed class OpCodeHandler_P_W : OpCodeHandlerModRM
	{
		// Token: 0x060025EA RID: 9706 RVA: 0x000805BA File Offset: 0x0007E7BA
		public OpCodeHandler_P_W(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025EB RID: 9707 RVA: 0x000805CC File Offset: 0x0007E7CC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.MM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003851 RID: 14417
		private readonly Code code;
	}
}
