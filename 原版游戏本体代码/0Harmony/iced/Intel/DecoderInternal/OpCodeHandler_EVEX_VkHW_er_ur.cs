using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006C9 RID: 1737
	internal sealed class OpCodeHandler_EVEX_VkHW_er_ur : OpCodeHandlerModRM
	{
		// Token: 0x0600246F RID: 9327 RVA: 0x00078108 File Offset: 0x00076308
		public OpCodeHandler_EVEX_VkHW_er_ur(Register baseReg, Code code, TupleType tupleType, bool canBroadcast)
		{
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x06002470 RID: 9328 RVA: 0x00078130 File Offset: 0x00076330
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			int regNum0 = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX);
			instruction.Op0Register = regNum0 + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				int regNum = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX);
				instruction.Op2Register = regNum + this.baseReg;
				if (decoder.invalidCheckMask != 0U && (regNum0 == (int)decoder.state.vvvv || regNum0 == regNum))
				{
					decoder.SetInvalidInstruction();
				}
				if ((decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
				{
					instruction.InternalRoundingControl = decoder.state.vectorLength + 1U;
					return;
				}
			}
			else
			{
				if (decoder.invalidCheckMask != 0U && regNum0 == (int)decoder.state.vvvv)
				{
					decoder.SetInvalidInstruction();
				}
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
		}

		// Token: 0x040036C1 RID: 14017
		private readonly Register baseReg;

		// Token: 0x040036C2 RID: 14018
		private readonly Code code;

		// Token: 0x040036C3 RID: 14019
		private readonly TupleType tupleType;

		// Token: 0x040036C4 RID: 14020
		private readonly bool canBroadcast;
	}
}
