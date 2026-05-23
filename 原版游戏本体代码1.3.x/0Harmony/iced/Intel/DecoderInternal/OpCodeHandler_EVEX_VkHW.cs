using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006DB RID: 1755
	internal sealed class OpCodeHandler_EVEX_VkHW : OpCodeHandlerModRM
	{
		// Token: 0x06002498 RID: 9368 RVA: 0x000795E1 File Offset: 0x000777E1
		public OpCodeHandler_EVEX_VkHW(Register baseReg, Code code, TupleType tupleType, bool canBroadcast)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.baseReg3 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x06002499 RID: 9369 RVA: 0x00079614 File Offset: 0x00077814
		public OpCodeHandler_EVEX_VkHW(Register baseReg1, Register baseReg2, Register baseReg3, Code code, TupleType tupleType, bool canBroadcast)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.baseReg3 = baseReg3;
			this.code = code;
			this.tupleType = tupleType;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x0600249A RID: 9370 RVA: 0x0007964C File Offset: 0x0007784C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg1;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg2;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg3;
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

		// Token: 0x04003709 RID: 14089
		private readonly Register baseReg1;

		// Token: 0x0400370A RID: 14090
		private readonly Register baseReg2;

		// Token: 0x0400370B RID: 14091
		private readonly Register baseReg3;

		// Token: 0x0400370C RID: 14092
		private readonly Code code;

		// Token: 0x0400370D RID: 14093
		private readonly TupleType tupleType;

		// Token: 0x0400370E RID: 14094
		private readonly bool canBroadcast;
	}
}
