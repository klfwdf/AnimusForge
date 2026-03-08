using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007D1 RID: 2001
	internal sealed class OpCodeHandler_VEX_VK_R_Ib : OpCodeHandlerModRM
	{
		// Token: 0x06002698 RID: 9880 RVA: 0x000845BB File Offset: 0x000827BB
		public OpCodeHandler_VEX_VK_R_Ib(Code code, Register gpr)
		{
			this.code = code;
			this.gpr = gpr;
		}

		// Token: 0x06002699 RID: 9881 RVA: 0x000845D4 File Offset: 0x000827D4
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (((decoder.state.vvvv_invalidCheck | decoder.state.zs.extraRegisterBase) & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.K0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.gpr;
			}
			else
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040038F3 RID: 14579
		private readonly Code code;

		// Token: 0x040038F4 RID: 14580
		private readonly Register gpr;
	}
}
