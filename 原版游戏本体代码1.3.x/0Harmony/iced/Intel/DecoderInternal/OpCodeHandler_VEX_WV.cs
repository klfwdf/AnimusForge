using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007A5 RID: 1957
	internal sealed class OpCodeHandler_VEX_WV : OpCodeHandlerModRM
	{
		// Token: 0x0600263C RID: 9788 RVA: 0x0008234C File Offset: 0x0008054C
		public OpCodeHandler_VEX_WV(Register baseReg, Code code)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
		}

		// Token: 0x0600263D RID: 9789 RVA: 0x0008236C File Offset: 0x0008056C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg1;
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg2;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003896 RID: 14486
		private readonly Register baseReg1;

		// Token: 0x04003897 RID: 14487
		private readonly Register baseReg2;

		// Token: 0x04003898 RID: 14488
		private readonly Code code;
	}
}
