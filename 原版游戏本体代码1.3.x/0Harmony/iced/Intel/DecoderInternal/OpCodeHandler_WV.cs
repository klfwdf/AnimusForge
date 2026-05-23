using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000773 RID: 1907
	internal sealed class OpCodeHandler_WV : OpCodeHandlerModRM
	{
		// Token: 0x060025D6 RID: 9686 RVA: 0x00080076 File Offset: 0x0007E276
		public OpCodeHandler_WV(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025D7 RID: 9687 RVA: 0x00080088 File Offset: 0x0007E288
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod < 3U)
			{
				instruction.Op0Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
				return;
			}
			instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
		}

		// Token: 0x04003847 RID: 14407
		private readonly Code code;
	}
}
