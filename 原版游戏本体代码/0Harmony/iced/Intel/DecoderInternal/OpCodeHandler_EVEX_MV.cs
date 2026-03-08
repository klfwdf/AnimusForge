using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006EA RID: 1770
	internal sealed class OpCodeHandler_EVEX_MV : OpCodeHandlerModRM
	{
		// Token: 0x060024B9 RID: 9401 RVA: 0x0007A7C7 File Offset: 0x000789C7
		public OpCodeHandler_EVEX_MV(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x060024BA RID: 9402 RVA: 0x0007A7E4 File Offset: 0x000789E4
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
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
			decoder.ReadOpMem(ref instruction, this.tupleType);
		}

		// Token: 0x04003748 RID: 14152
		private readonly Register baseReg;

		// Token: 0x04003749 RID: 14153
		private readonly Code code;

		// Token: 0x0400374A RID: 14154
		private readonly TupleType tupleType;
	}
}
