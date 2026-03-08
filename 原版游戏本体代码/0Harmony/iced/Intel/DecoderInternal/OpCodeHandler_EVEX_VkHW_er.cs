using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006C8 RID: 1736
	internal sealed class OpCodeHandler_EVEX_VkHW_er : OpCodeHandlerModRM
	{
		// Token: 0x0600246D RID: 9325 RVA: 0x00077FC1 File Offset: 0x000761C1
		public OpCodeHandler_EVEX_VkHW_er(Register baseReg, Code code, TupleType tupleType, bool onlySAE, bool canBroadcast)
		{
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.onlySAE = onlySAE;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x0600246E RID: 9326 RVA: 0x00077FF0 File Offset: 0x000761F0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg;
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

		// Token: 0x040036BC RID: 14012
		private readonly Register baseReg;

		// Token: 0x040036BD RID: 14013
		private readonly Code code;

		// Token: 0x040036BE RID: 14014
		private readonly TupleType tupleType;

		// Token: 0x040036BF RID: 14015
		private readonly bool onlySAE;

		// Token: 0x040036C0 RID: 14016
		private readonly bool canBroadcast;
	}
}
