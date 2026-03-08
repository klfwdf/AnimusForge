using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006E4 RID: 1764
	internal sealed class OpCodeHandler_EVEX_VHW : OpCodeHandlerModRM
	{
		// Token: 0x060024AC RID: 9388 RVA: 0x0007A0A4 File Offset: 0x000782A4
		public OpCodeHandler_EVEX_VHW(Register baseReg, Code codeR, Code codeM, TupleType tupleType)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.baseReg3 = baseReg;
			this.codeR = codeR;
			this.codeM = codeM;
			this.tupleType = tupleType;
		}

		// Token: 0x060024AD RID: 9389 RVA: 0x0007A0D7 File Offset: 0x000782D7
		public OpCodeHandler_EVEX_VHW(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.baseReg3 = baseReg;
			this.codeR = code;
			this.codeM = code;
			this.tupleType = tupleType;
		}

		// Token: 0x060024AE RID: 9390 RVA: 0x0007A10C File Offset: 0x0007830C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg1;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg2;
			if (decoder.state.mod == 3U)
			{
				instruction.InternalSetCodeNoCheck(this.codeR);
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg3;
				return;
			}
			instruction.InternalSetCodeNoCheck(this.codeM);
			instruction.Op2Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction, this.tupleType);
		}

		// Token: 0x0400372F RID: 14127
		private readonly Register baseReg1;

		// Token: 0x04003730 RID: 14128
		private readonly Register baseReg2;

		// Token: 0x04003731 RID: 14129
		private readonly Register baseReg3;

		// Token: 0x04003732 RID: 14130
		private readonly Code codeR;

		// Token: 0x04003733 RID: 14131
		private readonly Code codeM;

		// Token: 0x04003734 RID: 14132
		private readonly TupleType tupleType;
	}
}
