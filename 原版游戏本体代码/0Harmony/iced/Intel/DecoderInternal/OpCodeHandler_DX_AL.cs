using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000707 RID: 1799
	internal sealed class OpCodeHandler_DX_AL : OpCodeHandler
	{
		// Token: 0x060024F2 RID: 9458 RVA: 0x0007B51A File Offset: 0x0007971A
		public OpCodeHandler_DX_AL(Code code)
		{
			this.code = code;
		}

		// Token: 0x060024F3 RID: 9459 RVA: 0x0007B529 File Offset: 0x00079729
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = Register.DX;
			instruction.Op1Register = Register.AL;
		}

		// Token: 0x04003778 RID: 14200
		private readonly Code code;
	}
}
