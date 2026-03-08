using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007BA RID: 1978
	internal sealed class OpCodeHandler_VEX_VK_RK : OpCodeHandlerModRM
	{
		// Token: 0x0600266A RID: 9834 RVA: 0x000833D7 File Offset: 0x000815D7
		public OpCodeHandler_VEX_VK_RK(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600266B RID: 9835 RVA: 0x000833E8 File Offset: 0x000815E8
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
				instruction.Op1Register = (int)decoder.state.rm + Register.K0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x040038C8 RID: 14536
		private readonly Code code;
	}
}
