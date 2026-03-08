using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006E6 RID: 1766
	internal sealed class OpCodeHandler_EVEX_Gv_W_er : OpCodeHandlerModRM
	{
		// Token: 0x060024B1 RID: 9393 RVA: 0x0007A2D2 File Offset: 0x000784D2
		public OpCodeHandler_EVEX_Gv_W_er(Register baseReg, Code codeW0, Code codeW1, TupleType tupleType, bool onlySAE)
		{
			this.baseReg = baseReg;
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
			this.tupleType = tupleType;
			this.onlySAE = onlySAE;
		}

		// Token: 0x060024B2 RID: 9394 RVA: 0x0007A300 File Offset: 0x00078500
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & StateFlags.z) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa | (StateFlags)decoder.state.extraRegisterBaseEVEX) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.codeW1);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.codeW0);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.EAX;
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg;
				if ((decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
				{
					if (this.onlySAE)
					{
						instruction.InternalSetSuppressAllExceptions();
						return;
					}
					instruction.InternalRoundingControl = decoder.state.vectorLength + 1U;
					return;
				}
			}
			else
			{
				if ((decoder.state.zs.flags & StateFlags.b & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
				{
					decoder.SetInvalidInstruction();
				}
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
		}

		// Token: 0x04003738 RID: 14136
		private readonly Register baseReg;

		// Token: 0x04003739 RID: 14137
		private readonly Code codeW0;

		// Token: 0x0400373A RID: 14138
		private readonly Code codeW1;

		// Token: 0x0400373B RID: 14139
		private readonly TupleType tupleType;

		// Token: 0x0400373C RID: 14140
		private readonly bool onlySAE;
	}
}
