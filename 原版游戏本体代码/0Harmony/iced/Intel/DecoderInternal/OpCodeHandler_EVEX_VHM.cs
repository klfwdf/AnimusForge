using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006E5 RID: 1765
	internal sealed class OpCodeHandler_EVEX_VHM : OpCodeHandlerModRM
	{
		// Token: 0x060024AF RID: 9391 RVA: 0x0007A1F4 File Offset: 0x000783F4
		public OpCodeHandler_EVEX_VHM(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x060024B0 RID: 9392 RVA: 0x0007A214 File Offset: 0x00078414
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op2Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction, this.tupleType);
		}

		// Token: 0x04003735 RID: 14133
		private readonly Register baseReg;

		// Token: 0x04003736 RID: 14134
		private readonly Code code;

		// Token: 0x04003737 RID: 14135
		private readonly TupleType tupleType;
	}
}
