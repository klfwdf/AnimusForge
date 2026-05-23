using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006DA RID: 1754
	internal sealed class OpCodeHandler_EVEX_KkHWIb_sae : OpCodeHandlerModRM
	{
		// Token: 0x06002496 RID: 9366 RVA: 0x00079488 File Offset: 0x00077688
		public OpCodeHandler_EVEX_KkHWIb_sae(Register baseReg, Code code, TupleType tupleType, bool canBroadcast)
		{
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x06002497 RID: 9367 RVA: 0x000794B0 File Offset: 0x000776B0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & StateFlags.z) | (StateFlags)decoder.state.zs.extraRegisterBase | (StateFlags)decoder.state.extraRegisterBaseEVEX) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.K0;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg;
				if ((decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
				{
					instruction.InternalSetSuppressAllExceptions();
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

		// Token: 0x04003705 RID: 14085
		private readonly Register baseReg;

		// Token: 0x04003706 RID: 14086
		private readonly Code code;

		// Token: 0x04003707 RID: 14087
		private readonly TupleType tupleType;

		// Token: 0x04003708 RID: 14088
		private readonly bool canBroadcast;
	}
}
