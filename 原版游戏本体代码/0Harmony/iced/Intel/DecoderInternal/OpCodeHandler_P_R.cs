using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200077E RID: 1918
	internal sealed class OpCodeHandler_P_R : OpCodeHandlerModRM
	{
		// Token: 0x060025EC RID: 9708 RVA: 0x00080640 File Offset: 0x0007E840
		public OpCodeHandler_P_R(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025ED RID: 9709 RVA: 0x00080650 File Offset: 0x0007E850
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.MM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x04003852 RID: 14418
		private readonly Code code;
	}
}
