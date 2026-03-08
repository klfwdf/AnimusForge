using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000749 RID: 1865
	internal sealed class OpCodeHandler_Gv_Ev_Ib_REX : OpCodeHandlerModRM
	{
		// Token: 0x0600257C RID: 9596 RVA: 0x0007E5FC File Offset: 0x0007C7FC
		public OpCodeHandler_Gv_Ev_Ib_REX(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600257D RID: 9597 RVA: 0x0007E614 File Offset: 0x0007C814
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.EAX;
			}
			instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040037FF RID: 14335
		private readonly Code code32;

		// Token: 0x04003800 RID: 14336
		private readonly Code code64;
	}
}
