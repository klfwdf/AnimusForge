using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006F3 RID: 1779
	internal sealed class OpCodeHandler_STi : OpCodeHandlerModRM
	{
		// Token: 0x060024CC RID: 9420 RVA: 0x0007AF7A File Offset: 0x0007917A
		public OpCodeHandler_STi(Code code)
		{
			this.code = code;
		}

		// Token: 0x060024CD RID: 9421 RVA: 0x0007AF89 File Offset: 0x00079189
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = Register.ST0 + (int)decoder.state.rm;
		}

		// Token: 0x04003764 RID: 14180
		private readonly Code code;
	}
}
