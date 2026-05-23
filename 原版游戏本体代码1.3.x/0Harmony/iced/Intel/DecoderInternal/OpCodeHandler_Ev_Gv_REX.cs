using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200079A RID: 1946
	internal sealed class OpCodeHandler_Ev_Gv_REX : OpCodeHandlerModRM
	{
		// Token: 0x06002625 RID: 9765 RVA: 0x00081BDB File Offset: 0x0007FDDB
		public OpCodeHandler_Ev_Gv_REX(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002626 RID: 9766 RVA: 0x00081BF4 File Offset: 0x0007FDF4
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
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003882 RID: 14466
		private readonly Code code32;

		// Token: 0x04003883 RID: 14467
		private readonly Code code64;
	}
}
