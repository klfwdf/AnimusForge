using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006CC RID: 1740
	internal sealed class OpCodeHandler_EVEX_VkW : OpCodeHandlerModRM
	{
		// Token: 0x06002477 RID: 9335 RVA: 0x00078552 File Offset: 0x00076752
		public OpCodeHandler_EVEX_VkW(Register baseReg, Code code, TupleType tupleType, bool canBroadcast)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x06002478 RID: 9336 RVA: 0x0007857E File Offset: 0x0007677E
		public OpCodeHandler_EVEX_VkW(Register baseReg1, Register baseReg2, Code code, TupleType tupleType, bool canBroadcast)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.code = code;
			this.tupleType = tupleType;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x06002479 RID: 9337 RVA: 0x000785AC File Offset: 0x000767AC
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
				if ((decoder.state.zs.flags & StateFlags.b & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
				{
					decoder.SetInvalidInstruction();
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

		// Token: 0x040036CF RID: 14031
		private readonly Register baseReg1;

		// Token: 0x040036D0 RID: 14032
		private readonly Register baseReg2;

		// Token: 0x040036D1 RID: 14033
		private readonly Code code;

		// Token: 0x040036D2 RID: 14034
		private readonly TupleType tupleType;

		// Token: 0x040036D3 RID: 14035
		private readonly bool canBroadcast;
	}
}
