using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006D4 RID: 1748
	internal sealed class OpCodeHandler_EVEX_VW_er : OpCodeHandlerModRM
	{
		// Token: 0x0600248A RID: 9354 RVA: 0x00078EB1 File Offset: 0x000770B1
		public OpCodeHandler_EVEX_VW_er(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x0600248B RID: 9355 RVA: 0x00078ED8 File Offset: 0x000770D8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg1;
			if ((((decoder.state.zs.flags & StateFlags.z) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg2;
				if ((decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
				{
					instruction.InternalSetSuppressAllExceptions();
					return;
				}
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				if ((decoder.state.zs.flags & StateFlags.b & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
				{
					decoder.SetInvalidInstruction();
				}
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
		}

		// Token: 0x040036F2 RID: 14066
		private readonly Register baseReg1;

		// Token: 0x040036F3 RID: 14067
		private readonly Register baseReg2;

		// Token: 0x040036F4 RID: 14068
		private readonly Code code;

		// Token: 0x040036F5 RID: 14069
		private readonly TupleType tupleType;
	}
}
