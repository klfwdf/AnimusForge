using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006CA RID: 1738
	internal sealed class OpCodeHandler_EVEX_VkW_er : OpCodeHandlerModRM
	{
		// Token: 0x06002471 RID: 9329 RVA: 0x00078279 File Offset: 0x00076479
		public OpCodeHandler_EVEX_VkW_er(Register baseReg, Code code, TupleType tupleType, bool onlySAE)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.onlySAE = onlySAE;
			this.canBroadcast = true;
		}

		// Token: 0x06002472 RID: 9330 RVA: 0x000782AC File Offset: 0x000764AC
		public OpCodeHandler_EVEX_VkW_er(Register baseReg1, Register baseReg2, Code code, TupleType tupleType, bool onlySAE)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.code = code;
			this.tupleType = tupleType;
			this.onlySAE = onlySAE;
			this.canBroadcast = true;
		}

		// Token: 0x06002473 RID: 9331 RVA: 0x000782E0 File Offset: 0x000764E0
		public OpCodeHandler_EVEX_VkW_er(Register baseReg1, Register baseReg2, Code code, TupleType tupleType, bool onlySAE, bool canBroadcast)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.code = code;
			this.tupleType = tupleType;
			this.onlySAE = onlySAE;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x06002474 RID: 9332 RVA: 0x00078318 File Offset: 0x00076518
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg1;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg2;
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
				instruction.Op1Kind = OpKind.Memory;
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

		// Token: 0x040036C5 RID: 14021
		private readonly Register baseReg1;

		// Token: 0x040036C6 RID: 14022
		private readonly Register baseReg2;

		// Token: 0x040036C7 RID: 14023
		private readonly Code code;

		// Token: 0x040036C8 RID: 14024
		private readonly TupleType tupleType;

		// Token: 0x040036C9 RID: 14025
		private readonly bool onlySAE;

		// Token: 0x040036CA RID: 14026
		private readonly bool canBroadcast;
	}
}
