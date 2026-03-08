using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006E2 RID: 1762
	internal sealed class OpCodeHandler_EVEX_WkHV : OpCodeHandlerModRM
	{
		// Token: 0x060024A8 RID: 9384 RVA: 0x00079ED0 File Offset: 0x000780D0
		public OpCodeHandler_EVEX_WkHV(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x060024A9 RID: 9385 RVA: 0x00079EE8 File Offset: 0x000780E8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg;
			if ((decoder.state.zs.flags & StateFlags.b & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			instruction.Op2Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
		}

		// Token: 0x0400372A RID: 14122
		private readonly Register baseReg;

		// Token: 0x0400372B RID: 14123
		private readonly Code code;
	}
}
