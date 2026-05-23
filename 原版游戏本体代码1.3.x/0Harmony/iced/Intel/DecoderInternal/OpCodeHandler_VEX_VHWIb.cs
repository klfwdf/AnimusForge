using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007B3 RID: 1971
	internal sealed class OpCodeHandler_VEX_VHWIb : OpCodeHandlerModRM
	{
		// Token: 0x0600265B RID: 9819 RVA: 0x00082E13 File Offset: 0x00081013
		public OpCodeHandler_VEX_VHWIb(Register baseReg, Code code)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.baseReg3 = baseReg;
			this.code = code;
		}

		// Token: 0x0600265C RID: 9820 RVA: 0x00082E37 File Offset: 0x00081037
		public OpCodeHandler_VEX_VHWIb(Register baseReg1, Register baseReg2, Register baseReg3, Code code)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.baseReg3 = baseReg3;
			this.code = code;
		}

		// Token: 0x0600265D RID: 9821 RVA: 0x00082E5C File Offset: 0x0008105C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg1;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg2;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg3;
			}
			else
			{
				instruction.Op2Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			instruction.Op3Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040038B9 RID: 14521
		private readonly Register baseReg1;

		// Token: 0x040038BA RID: 14522
		private readonly Register baseReg2;

		// Token: 0x040038BB RID: 14523
		private readonly Register baseReg3;

		// Token: 0x040038BC RID: 14524
		private readonly Code code;
	}
}
