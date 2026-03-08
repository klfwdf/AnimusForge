using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006E8 RID: 1768
	internal sealed class OpCodeHandler_EVEX_Ev_VX : OpCodeHandlerModRM
	{
		// Token: 0x060024B5 RID: 9397 RVA: 0x0007A59A File Offset: 0x0007879A
		public OpCodeHandler_EVEX_Ev_VX(Code code32, Code code64, TupleType tupleTypeW0, TupleType tupleTypeW1)
		{
			this.code32 = code32;
			this.code64 = code64;
			this.tupleTypeW0 = tupleTypeW0;
			this.tupleTypeW1 = tupleTypeW1;
		}

		// Token: 0x060024B6 RID: 9398 RVA: 0x0007A5C0 File Offset: 0x000787C0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + Register.XMM0;
			TupleType tupleType;
			Register gpr;
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				tupleType = this.tupleTypeW1;
				gpr = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				tupleType = this.tupleTypeW0;
				gpr = Register.EAX;
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction, tupleType);
		}

		// Token: 0x04003741 RID: 14145
		private readonly Code code32;

		// Token: 0x04003742 RID: 14146
		private readonly Code code64;

		// Token: 0x04003743 RID: 14147
		private readonly TupleType tupleTypeW0;

		// Token: 0x04003744 RID: 14148
		private readonly TupleType tupleTypeW1;
	}
}
