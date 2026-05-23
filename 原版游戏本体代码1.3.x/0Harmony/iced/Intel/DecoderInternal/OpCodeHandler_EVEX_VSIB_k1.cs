using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006EE RID: 1774
	internal sealed class OpCodeHandler_EVEX_VSIB_k1 : OpCodeHandlerModRM
	{
		// Token: 0x060024C2 RID: 9410 RVA: 0x0007AB99 File Offset: 0x00078D99
		public OpCodeHandler_EVEX_VSIB_k1(Register vsibIndex, Code code, TupleType tupleType)
		{
			this.vsibIndex = vsibIndex;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x060024C3 RID: 9411 RVA: 0x0007ABB8 File Offset: 0x00078DB8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.invalidCheckMask != 0U && (((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)(decoder.state.vvvv_invalidCheck & 15U)) != (StateFlags)0U || decoder.state.aaa == 0U))
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem_VSIB(ref instruction, this.vsibIndex, this.tupleType);
		}

		// Token: 0x04003756 RID: 14166
		private readonly Register vsibIndex;

		// Token: 0x04003757 RID: 14167
		private readonly Code code;

		// Token: 0x04003758 RID: 14168
		private readonly TupleType tupleType;
	}
}
