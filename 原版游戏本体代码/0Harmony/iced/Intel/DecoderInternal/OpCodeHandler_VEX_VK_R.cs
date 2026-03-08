using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007BE RID: 1982
	internal sealed class OpCodeHandler_VEX_VK_R : OpCodeHandlerModRM
	{
		// Token: 0x06002672 RID: 9842 RVA: 0x0008364C File Offset: 0x0008184C
		public OpCodeHandler_VEX_VK_R(Code code, Register gpr)
		{
			this.code = code;
			this.gpr = gpr;
		}

		// Token: 0x06002673 RID: 9843 RVA: 0x00083664 File Offset: 0x00081864
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
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x040038CC RID: 14540
		private readonly Code code;

		// Token: 0x040038CD RID: 14541
		private readonly Register gpr;
	}
}
