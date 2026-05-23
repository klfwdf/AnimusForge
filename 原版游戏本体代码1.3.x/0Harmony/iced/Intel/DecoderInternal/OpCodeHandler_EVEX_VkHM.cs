using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006DC RID: 1756
	internal sealed class OpCodeHandler_EVEX_VkHM : OpCodeHandlerModRM
	{
		// Token: 0x0600249B RID: 9371 RVA: 0x0007974F File Offset: 0x0007794F
		public OpCodeHandler_EVEX_VkHM(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x0600249C RID: 9372 RVA: 0x00079774 File Offset: 0x00077974
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg1;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg2;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op2Kind = OpKind.Memory;
			if ((decoder.state.zs.flags & StateFlags.b & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			decoder.ReadOpMem(ref instruction, this.tupleType);
		}

		// Token: 0x0400370F RID: 14095
		private readonly Register baseReg1;

		// Token: 0x04003710 RID: 14096
		private readonly Register baseReg2;

		// Token: 0x04003711 RID: 14097
		private readonly Code code;

		// Token: 0x04003712 RID: 14098
		private readonly TupleType tupleType;
	}
}
