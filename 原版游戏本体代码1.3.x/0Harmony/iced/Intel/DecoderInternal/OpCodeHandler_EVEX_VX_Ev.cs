using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006E7 RID: 1767
	internal sealed class OpCodeHandler_EVEX_VX_Ev : OpCodeHandlerModRM
	{
		// Token: 0x060024B3 RID: 9395 RVA: 0x0007A471 File Offset: 0x00078671
		public OpCodeHandler_EVEX_VX_Ev(Code code32, Code code64, TupleType tupleTypeW0, TupleType tupleTypeW1)
		{
			this.code32 = code32;
			this.code64 = code64;
			this.tupleTypeW0 = tupleTypeW0;
			this.tupleTypeW1 = tupleTypeW1;
		}

		// Token: 0x060024B4 RID: 9396 RVA: 0x0007A498 File Offset: 0x00078698
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
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
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction, tupleType);
		}

		// Token: 0x0400373D RID: 14141
		private readonly Code code32;

		// Token: 0x0400373E RID: 14142
		private readonly Code code64;

		// Token: 0x0400373F RID: 14143
		private readonly TupleType tupleTypeW0;

		// Token: 0x04003740 RID: 14144
		private readonly TupleType tupleTypeW1;
	}
}
