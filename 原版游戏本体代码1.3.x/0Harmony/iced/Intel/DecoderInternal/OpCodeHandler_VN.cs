using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000795 RID: 1941
	internal sealed class OpCodeHandler_VN : OpCodeHandlerModRM
	{
		// Token: 0x0600261B RID: 9755 RVA: 0x000817FF File Offset: 0x0007F9FF
		public OpCodeHandler_VN(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600261C RID: 9756 RVA: 0x00081810 File Offset: 0x0007FA10
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)decoder.state.rm + Register.MM0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x0400387B RID: 14459
		private readonly Code code;
	}
}
