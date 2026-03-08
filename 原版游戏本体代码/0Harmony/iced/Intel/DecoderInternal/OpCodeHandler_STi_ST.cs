using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006F2 RID: 1778
	internal sealed class OpCodeHandler_STi_ST : OpCodeHandlerModRM
	{
		// Token: 0x060024CA RID: 9418 RVA: 0x0007AF3B File Offset: 0x0007913B
		public OpCodeHandler_STi_ST(Code code)
		{
			this.code = code;
		}

		// Token: 0x060024CB RID: 9419 RVA: 0x0007AF4A File Offset: 0x0007914A
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = Register.ST0 + (int)decoder.state.rm;
			instruction.Op1Register = Register.ST0;
		}

		// Token: 0x04003763 RID: 14179
		private readonly Code code;
	}
}
