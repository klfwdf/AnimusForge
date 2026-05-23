using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000719 RID: 1817
	internal sealed class OpCodeHandler_Rq : OpCodeHandlerModRM
	{
		// Token: 0x06002519 RID: 9497 RVA: 0x0007C08D File Offset: 0x0007A28D
		public OpCodeHandler_Rq(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600251A RID: 9498 RVA: 0x0007C09C File Offset: 0x0007A29C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.RAX;
		}

		// Token: 0x04003796 RID: 14230
		private readonly Code code;
	}
}
