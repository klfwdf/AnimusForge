using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006E0 RID: 1760
	internal sealed class OpCodeHandler_EVEX_KP1HW : OpCodeHandlerModRM
	{
		// Token: 0x060024A4 RID: 9380 RVA: 0x00079C38 File Offset: 0x00077E38
		public OpCodeHandler_EVEX_KP1HW(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x060024A5 RID: 9381 RVA: 0x00079C58 File Offset: 0x00077E58
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.K0;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if ((((decoder.state.zs.flags & StateFlags.z) | (StateFlags)decoder.state.aaa | (StateFlags)decoder.state.zs.extraRegisterBase | (StateFlags)decoder.state.extraRegisterBaseEVEX) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg;
				if ((decoder.state.zs.flags & StateFlags.b & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
				{
					decoder.SetInvalidInstruction();
					return;
				}
			}
			else
			{
				instruction.Op2Kind = OpKind.Memory;
				if ((decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
				{
					instruction.InternalSetIsBroadcast();
				}
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
		}

		// Token: 0x04003723 RID: 14115
		private readonly Register baseReg;

		// Token: 0x04003724 RID: 14116
		private readonly Code code;

		// Token: 0x04003725 RID: 14117
		private readonly TupleType tupleType;
	}
}
