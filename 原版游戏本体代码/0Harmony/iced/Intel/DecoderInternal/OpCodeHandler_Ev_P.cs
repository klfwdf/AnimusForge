using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000781 RID: 1921
	internal sealed class OpCodeHandler_Ev_P : OpCodeHandlerModRM
	{
		// Token: 0x060025F2 RID: 9714 RVA: 0x0008083B File Offset: 0x0007EA3B
		public OpCodeHandler_Ev_P(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x060025F3 RID: 9715 RVA: 0x00080854 File Offset: 0x0007EA54
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.Op1Register = (int)decoder.state.reg + Register.MM0;
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
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003857 RID: 14423
		private readonly Code code32;

		// Token: 0x04003858 RID: 14424
		private readonly Code code64;
	}
}
