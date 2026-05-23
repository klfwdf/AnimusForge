using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006DE RID: 1758
	internal sealed class OpCodeHandler_EVEX_VkHWIb_er : OpCodeHandlerModRM
	{
		// Token: 0x060024A0 RID: 9376 RVA: 0x000799A7 File Offset: 0x00077BA7
		public OpCodeHandler_EVEX_VkHWIb_er(Register baseReg, Code code, TupleType tupleType, bool canBroadcast)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.baseReg3 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x060024A1 RID: 9377 RVA: 0x000799DC File Offset: 0x00077BDC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg1;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg2;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg3;
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

		// Token: 0x04003719 RID: 14105
		private readonly Register baseReg1;

		// Token: 0x0400371A RID: 14106
		private readonly Register baseReg2;

		// Token: 0x0400371B RID: 14107
		private readonly Register baseReg3;

		// Token: 0x0400371C RID: 14108
		private readonly Code code;

		// Token: 0x0400371D RID: 14109
		private readonly TupleType tupleType;

		// Token: 0x0400371E RID: 14110
		private readonly bool canBroadcast;
	}
}
