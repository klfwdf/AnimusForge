using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000789 RID: 1929
	internal sealed class OpCodeHandler_VX_Ev : OpCodeHandlerModRM
	{
		// Token: 0x06002603 RID: 9731 RVA: 0x00080E09 File Offset: 0x0007F009
		public OpCodeHandler_VX_Ev(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002604 RID: 9732 RVA: 0x00080E20 File Offset: 0x0007F020
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			Register gpr;
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				gpr = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				gpr = Register.EAX;
			}
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003864 RID: 14436
		private readonly Code code32;

		// Token: 0x04003865 RID: 14437
		private readonly Code code64;
	}
}
