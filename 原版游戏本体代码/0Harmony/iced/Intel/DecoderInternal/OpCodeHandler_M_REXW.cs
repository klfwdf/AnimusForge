using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000770 RID: 1904
	internal sealed class OpCodeHandler_M_REXW : OpCodeHandlerModRM
	{
		// Token: 0x060025CE RID: 9678 RVA: 0x0007FE60 File Offset: 0x0007E060
		public OpCodeHandler_M_REXW(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x060025CF RID: 9679 RVA: 0x0007FE76 File Offset: 0x0007E076
		public OpCodeHandler_M_REXW(Code code32, Code code64, HandlerFlags flags32, HandlerFlags flags64)
		{
			this.code32 = code32;
			this.code64 = code64;
			this.flags32 = flags32;
			this.flags64 = flags64;
		}

		// Token: 0x060025D0 RID: 9680 RVA: 0x0007FE9C File Offset: 0x0007E09C
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
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			HandlerFlags flags = (((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U) ? this.flags64 : this.flags32);
			if ((flags & (HandlerFlags.Xacquire | HandlerFlags.Xrelease)) != HandlerFlags.None)
			{
				decoder.SetXacquireXrelease(ref instruction);
			}
			decoder.state.zs.flags = decoder.state.zs.flags | (StateFlags)((flags & HandlerFlags.Lock) << 10);
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003840 RID: 14400
		private readonly Code code32;

		// Token: 0x04003841 RID: 14401
		private readonly Code code64;

		// Token: 0x04003842 RID: 14402
		private readonly HandlerFlags flags32;

		// Token: 0x04003843 RID: 14403
		private readonly HandlerFlags flags64;
	}
}
