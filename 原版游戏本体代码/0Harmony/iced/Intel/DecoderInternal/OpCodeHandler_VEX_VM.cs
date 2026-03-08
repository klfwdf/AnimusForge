using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007A6 RID: 1958
	internal sealed class OpCodeHandler_VEX_VM : OpCodeHandlerModRM
	{
		// Token: 0x0600263E RID: 9790 RVA: 0x00082410 File Offset: 0x00080610
		public OpCodeHandler_VEX_VM(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x0600263F RID: 9791 RVA: 0x00082428 File Offset: 0x00080628
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003899 RID: 14489
		private readonly Register baseReg;

		// Token: 0x0400389A RID: 14490
		private readonly Code code;
	}
}
