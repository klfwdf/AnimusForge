using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007AE RID: 1966
	internal sealed class OpCodeHandler_VEX_VHW : OpCodeHandlerModRM
	{
		// Token: 0x0600264F RID: 9807 RVA: 0x00082A60 File Offset: 0x00080C60
		public OpCodeHandler_VEX_VHW(Register baseReg, Code codeR, Code codeM)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.baseReg3 = baseReg;
			this.codeR = codeR;
			this.codeM = codeM;
		}

		// Token: 0x06002650 RID: 9808 RVA: 0x00082A8B File Offset: 0x00080C8B
		public OpCodeHandler_VEX_VHW(Register baseReg, Code code)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.baseReg3 = baseReg;
			this.codeR = code;
			this.codeM = code;
		}

		// Token: 0x06002651 RID: 9809 RVA: 0x00082AB6 File Offset: 0x00080CB6
		public OpCodeHandler_VEX_VHW(Register baseReg1, Register baseReg2, Register baseReg3, Code code)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.baseReg3 = baseReg3;
			this.codeR = code;
			this.codeM = code;
		}

		// Token: 0x06002652 RID: 9810 RVA: 0x00082AE4 File Offset: 0x00080CE4
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg1;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg2;
			if (decoder.state.mod == 3U)
			{
				instruction.InternalSetCodeNoCheck(this.codeR);
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg3;
				return;
			}
			instruction.InternalSetCodeNoCheck(this.codeM);
			instruction.Op2Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038AC RID: 14508
		private readonly Register baseReg1;

		// Token: 0x040038AD RID: 14509
		private readonly Register baseReg2;

		// Token: 0x040038AE RID: 14510
		private readonly Register baseReg3;

		// Token: 0x040038AF RID: 14511
		private readonly Code codeR;

		// Token: 0x040038B0 RID: 14512
		private readonly Code codeM;
	}
}
