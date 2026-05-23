using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000793 RID: 1939
	internal sealed class OpCodeHandler_Gv_N_Ib_REX : OpCodeHandlerModRM
	{
		// Token: 0x06002617 RID: 9751 RVA: 0x0008161E File Offset: 0x0007F81E
		public OpCodeHandler_Gv_N_Ib_REX(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002618 RID: 9752 RVA: 0x00081634 File Offset: 0x0007F834
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
			}
			else
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x04003877 RID: 14455
		private readonly Code code32;

		// Token: 0x04003878 RID: 14456
		private readonly Code code64;
	}
}
