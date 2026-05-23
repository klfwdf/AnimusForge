using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200078C RID: 1932
	internal sealed class OpCodeHandler_Gv_RX : OpCodeHandlerModRM
	{
		// Token: 0x06002609 RID: 9737 RVA: 0x00081069 File Offset: 0x0007F269
		public OpCodeHandler_Gv_RX(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600260A RID: 9738 RVA: 0x00081080 File Offset: 0x0007F280
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
			}
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
			{
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.RAX;
			}
			else
			{
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.EAX;
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x0400386A RID: 14442
		private readonly Code code32;

		// Token: 0x0400386B RID: 14443
		private readonly Code code64;
	}
}
