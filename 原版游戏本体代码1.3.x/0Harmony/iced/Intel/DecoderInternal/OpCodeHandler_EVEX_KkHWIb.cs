using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006E1 RID: 1761
	internal sealed class OpCodeHandler_EVEX_KkHWIb : OpCodeHandlerModRM
	{
		// Token: 0x060024A6 RID: 9382 RVA: 0x00079D70 File Offset: 0x00077F70
		public OpCodeHandler_EVEX_KkHWIb(Register baseReg, Code code, TupleType tupleType, bool canBroadcast)
		{
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x060024A7 RID: 9383 RVA: 0x00079D98 File Offset: 0x00077F98
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.K0;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if ((((decoder.state.zs.flags & StateFlags.z) | (StateFlags)decoder.state.zs.extraRegisterBase | (StateFlags)decoder.state.extraRegisterBaseEVEX) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg;
				if ((decoder.state.zs.flags & StateFlags.b & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
				{
					decoder.SetInvalidInstruction();
				}
			}
			else
			{
				instruction.Op2Kind = OpKind.Memory;
				if ((decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
				{
					if (this.canBroadcast)
					{
						instruction.InternalSetIsBroadcast();
					}
					else if (decoder.invalidCheckMask != 0U)
					{
						decoder.SetInvalidInstruction();
					}
				}
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
			instruction.Op3Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x04003726 RID: 14118
		private readonly Register baseReg;

		// Token: 0x04003727 RID: 14119
		private readonly Code code;

		// Token: 0x04003728 RID: 14120
		private readonly TupleType tupleType;

		// Token: 0x04003729 RID: 14121
		private readonly bool canBroadcast;
	}
}
