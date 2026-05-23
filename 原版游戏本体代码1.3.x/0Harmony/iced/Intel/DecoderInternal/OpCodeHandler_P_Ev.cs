using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200077F RID: 1919
	internal sealed class OpCodeHandler_P_Ev : OpCodeHandlerModRM
	{
		// Token: 0x060025EE RID: 9710 RVA: 0x000806BA File Offset: 0x0007E8BA
		public OpCodeHandler_P_Ev(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x060025EF RID: 9711 RVA: 0x000806D0 File Offset: 0x0007E8D0
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
			instruction.Op0Register = (int)decoder.state.reg + Register.MM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003853 RID: 14419
		private readonly Code code32;

		// Token: 0x04003854 RID: 14420
		private readonly Code code64;
	}
}
