using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000706 RID: 1798
	internal sealed class OpCodeHandler_AL_DX : OpCodeHandler
	{
		// Token: 0x060024F0 RID: 9456 RVA: 0x0007B4EE File Offset: 0x000796EE
		public OpCodeHandler_AL_DX(Code code)
		{
			this.code = code;
		}

		// Token: 0x060024F1 RID: 9457 RVA: 0x0007B4FD File Offset: 0x000796FD
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = Register.AL;
			instruction.Op1Register = Register.DX;
		}

		// Token: 0x04003777 RID: 14199
		private readonly Code code;
	}
}
