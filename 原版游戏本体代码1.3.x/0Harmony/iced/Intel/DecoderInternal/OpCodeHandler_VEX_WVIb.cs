using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007AC RID: 1964
	internal sealed class OpCodeHandler_VEX_WVIb : OpCodeHandlerModRM
	{
		// Token: 0x0600264B RID: 9803 RVA: 0x0008288B File Offset: 0x00080A8B
		public OpCodeHandler_VEX_WVIb(Register baseReg1, Register baseReg2, Code code)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.code = code;
		}

		// Token: 0x0600264C RID: 9804 RVA: 0x000828A8 File Offset: 0x00080AA8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg1;
			}
			else
			{
				instruction.Op0Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg2;
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040038A6 RID: 14502
		private readonly Register baseReg1;

		// Token: 0x040038A7 RID: 14503
		private readonly Register baseReg2;

		// Token: 0x040038A8 RID: 14504
		private readonly Code code;
	}
}
