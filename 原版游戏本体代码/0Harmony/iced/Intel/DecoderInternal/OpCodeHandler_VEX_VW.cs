using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007A2 RID: 1954
	internal sealed class OpCodeHandler_VEX_VW : OpCodeHandlerModRM
	{
		// Token: 0x06002635 RID: 9781 RVA: 0x000820AE File Offset: 0x000802AE
		public OpCodeHandler_VEX_VW(Register baseReg, Code code)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
		}

		// Token: 0x06002636 RID: 9782 RVA: 0x000820CB File Offset: 0x000802CB
		public OpCodeHandler_VEX_VW(Register baseReg1, Register baseReg2, Code code)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.code = code;
		}

		// Token: 0x06002637 RID: 9783 RVA: 0x000820E8 File Offset: 0x000802E8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg1;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg2;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400388F RID: 14479
		private readonly Register baseReg1;

		// Token: 0x04003890 RID: 14480
		private readonly Register baseReg2;

		// Token: 0x04003891 RID: 14481
		private readonly Code code;
	}
}
