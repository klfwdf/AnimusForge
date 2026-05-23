using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000794 RID: 1940
	internal sealed class OpCodeHandler_Gv_N : OpCodeHandlerModRM
	{
		// Token: 0x06002619 RID: 9753 RVA: 0x00081717 File Offset: 0x0007F917
		public OpCodeHandler_Gv_N(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600261A RID: 9754 RVA: 0x00081730 File Offset: 0x0007F930
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
				instruction.Op1Register = (int)decoder.state.rm + Register.MM0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x04003879 RID: 14457
		private readonly Code code32;

		// Token: 0x0400387A RID: 14458
		private readonly Code code64;
	}
}
