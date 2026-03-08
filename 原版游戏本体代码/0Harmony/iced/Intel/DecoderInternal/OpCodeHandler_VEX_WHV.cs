using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007B0 RID: 1968
	internal sealed class OpCodeHandler_VEX_WHV : OpCodeHandlerModRM
	{
		// Token: 0x06002655 RID: 9813 RVA: 0x00082C4A File Offset: 0x00080E4A
		public OpCodeHandler_VEX_WHV(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.codeR = code;
		}

		// Token: 0x06002656 RID: 9814 RVA: 0x00082C60 File Offset: 0x00080E60
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.codeR);
			instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			instruction.Op2Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
		}

		// Token: 0x040038B3 RID: 14515
		private readonly Register baseReg;

		// Token: 0x040038B4 RID: 14516
		private readonly Code codeR;
	}
}
