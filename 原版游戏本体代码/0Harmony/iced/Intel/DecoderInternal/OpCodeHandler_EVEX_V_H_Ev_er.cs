using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006C5 RID: 1733
	internal sealed class OpCodeHandler_EVEX_V_H_Ev_er : OpCodeHandlerModRM
	{
		// Token: 0x06002467 RID: 9319 RVA: 0x00077B59 File Offset: 0x00075D59
		public OpCodeHandler_EVEX_V_H_Ev_er(Register baseReg, Code codeW0, Code codeW1, TupleType tupleTypeW0, TupleType tupleTypeW1)
		{
			this.baseReg = baseReg;
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
			this.tupleTypeW0 = tupleTypeW0;
			this.tupleTypeW1 = tupleTypeW1;
		}

		// Token: 0x06002468 RID: 9320 RVA: 0x00077B88 File Offset: 0x00075D88
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & StateFlags.z) | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			TupleType tupleType;
			Register gpr;
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.codeW1);
				tupleType = this.tupleTypeW1;
				gpr = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.codeW0);
				tupleType = this.tupleTypeW0;
				gpr = Register.EAX;
			}
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				if ((decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
				{
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
				instruction.Op2Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction, tupleType);
			}
		}

		// Token: 0x040036AD RID: 13997
		private readonly Register baseReg;

		// Token: 0x040036AE RID: 13998
		private readonly Code codeW0;

		// Token: 0x040036AF RID: 13999
		private readonly Code codeW1;

		// Token: 0x040036B0 RID: 14000
		private readonly TupleType tupleTypeW0;

		// Token: 0x040036B1 RID: 14001
		private readonly TupleType tupleTypeW1;
	}
}
