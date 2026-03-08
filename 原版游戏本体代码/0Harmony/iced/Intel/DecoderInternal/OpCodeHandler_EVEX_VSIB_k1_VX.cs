using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006ED RID: 1773
	internal sealed class OpCodeHandler_EVEX_VSIB_k1_VX : OpCodeHandlerModRM
	{
		// Token: 0x060024C0 RID: 9408 RVA: 0x0007AAB4 File Offset: 0x00078CB4
		public OpCodeHandler_EVEX_VSIB_k1_VX(Register vsibIndex, Register baseReg, Code code, TupleType tupleType)
		{
			this.vsibIndex = vsibIndex;
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x060024C1 RID: 9409 RVA: 0x0007AADC File Offset: 0x00078CDC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.invalidCheckMask != 0U && (((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)(decoder.state.vvvv_invalidCheck & 15U)) != (StateFlags)0U || decoder.state.aaa == 0U))
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem_VSIB(ref instruction, this.vsibIndex, this.tupleType);
		}

		// Token: 0x04003752 RID: 14162
		private readonly Register vsibIndex;

		// Token: 0x04003753 RID: 14163
		private readonly Register baseReg;

		// Token: 0x04003754 RID: 14164
		private readonly Code code;

		// Token: 0x04003755 RID: 14165
		private readonly TupleType tupleType;
	}
}
