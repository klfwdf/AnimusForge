using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007B4 RID: 1972
	internal sealed class OpCodeHandler_VEX_HRIb : OpCodeHandlerModRM
	{
		// Token: 0x0600265E RID: 9822 RVA: 0x00082F12 File Offset: 0x00081112
		public OpCodeHandler_VEX_HRIb(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x0600265F RID: 9823 RVA: 0x00082F28 File Offset: 0x00081128
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg;
			}
			else
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040038BD RID: 14525
		private readonly Register baseReg;

		// Token: 0x040038BE RID: 14526
		private readonly Code code;
	}
}
