using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006D8 RID: 1752
	internal sealed class OpCodeHandler_EVEX_VK : OpCodeHandlerModRM
	{
		// Token: 0x06002492 RID: 9362 RVA: 0x000792E6 File Offset: 0x000774E6
		public OpCodeHandler_EVEX_VK(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x06002493 RID: 9363 RVA: 0x000792FC File Offset: 0x000774FC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)decoder.state.rm + Register.K0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x04003701 RID: 14081
		private readonly Register baseReg;

		// Token: 0x04003702 RID: 14082
		private readonly Code code;
	}
}
