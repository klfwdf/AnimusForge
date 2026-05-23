using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007B9 RID: 1977
	internal sealed class OpCodeHandler_VEX_VK_HK_RK : OpCodeHandlerModRM
	{
		// Token: 0x06002668 RID: 9832 RVA: 0x00083323 File Offset: 0x00081523
		public OpCodeHandler_VEX_VK_HK_RK(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002669 RID: 9833 RVA: 0x00083334 File Offset: 0x00081534
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.invalidCheckMask != 0U && (decoder.state.vvvv > 7U || decoder.state.zs.extraRegisterBase != 0U))
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.K0;
			instruction.Op1Register = (int)(decoder.state.vvvv & 7U) + Register.K0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)decoder.state.rm + Register.K0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x040038C7 RID: 14535
		private readonly Code code;
	}
}
