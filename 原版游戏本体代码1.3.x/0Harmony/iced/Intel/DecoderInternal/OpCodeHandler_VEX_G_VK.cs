using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007BF RID: 1983
	internal sealed class OpCodeHandler_VEX_G_VK : OpCodeHandlerModRM
	{
		// Token: 0x06002674 RID: 9844 RVA: 0x000836FD File Offset: 0x000818FD
		public OpCodeHandler_VEX_G_VK(Code code, Register gpr)
		{
			this.code = code;
			this.gpr = gpr;
		}

		// Token: 0x06002675 RID: 9845 RVA: 0x00083714 File Offset: 0x00081914
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.gpr;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)decoder.state.rm + Register.K0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x040038CE RID: 14542
		private readonly Code code;

		// Token: 0x040038CF RID: 14543
		private readonly Register gpr;
	}
}
