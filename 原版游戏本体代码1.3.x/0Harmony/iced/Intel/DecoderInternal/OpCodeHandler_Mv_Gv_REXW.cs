using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000792 RID: 1938
	internal sealed class OpCodeHandler_Mv_Gv_REXW : OpCodeHandlerModRM
	{
		// Token: 0x06002615 RID: 9749 RVA: 0x0008155A File Offset: 0x0007F75A
		public OpCodeHandler_Mv_Gv_REXW(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002616 RID: 9750 RVA: 0x00081570 File Offset: 0x0007F770
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.EAX;
			}
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003875 RID: 14453
		private readonly Code code32;

		// Token: 0x04003876 RID: 14454
		private readonly Code code64;
	}
}
