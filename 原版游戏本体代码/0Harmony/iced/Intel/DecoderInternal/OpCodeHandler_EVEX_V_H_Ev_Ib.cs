using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006C6 RID: 1734
	internal sealed class OpCodeHandler_EVEX_V_H_Ev_Ib : OpCodeHandlerModRM
	{
		// Token: 0x06002469 RID: 9321 RVA: 0x00077CE4 File Offset: 0x00075EE4
		public OpCodeHandler_EVEX_V_H_Ev_Ib(Register baseReg, Code codeW0, Code codeW1, TupleType tupleTypeW0, TupleType tupleTypeW1)
		{
			this.baseReg = baseReg;
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
			this.tupleTypeW0 = tupleTypeW0;
			this.tupleTypeW1 = tupleTypeW1;
		}

		// Token: 0x0600246A RID: 9322 RVA: 0x00077D14 File Offset: 0x00075F14
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			Register gpr;
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.codeW1);
				gpr = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.codeW0);
				gpr = Register.EAX;
			}
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
			}
			else
			{
				instruction.Op2Kind = OpKind.Memory;
				if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
				{
					decoder.ReadOpMem(ref instruction, this.tupleTypeW1);
				}
				else
				{
					decoder.ReadOpMem(ref instruction, this.tupleTypeW0);
				}
			}
			instruction.Op3Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040036B2 RID: 14002
		private readonly Register baseReg;

		// Token: 0x040036B3 RID: 14003
		private readonly Code codeW0;

		// Token: 0x040036B4 RID: 14004
		private readonly Code codeW1;

		// Token: 0x040036B5 RID: 14005
		private readonly TupleType tupleTypeW0;

		// Token: 0x040036B6 RID: 14006
		private readonly TupleType tupleTypeW1;
	}
}
