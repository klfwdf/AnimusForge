using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006F1 RID: 1777
	internal sealed class OpCodeHandler_ST_STi : OpCodeHandlerModRM
	{
		// Token: 0x060024C8 RID: 9416 RVA: 0x0007AEFC File Offset: 0x000790FC
		public OpCodeHandler_ST_STi(Code code)
		{
			this.code = code;
		}

		// Token: 0x060024C9 RID: 9417 RVA: 0x0007AF0B File Offset: 0x0007910B
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = Register.ST0;
			instruction.Op1Register = Register.ST0 + (int)decoder.state.rm;
		}

		// Token: 0x04003762 RID: 14178
		private readonly Code code;
	}
}
