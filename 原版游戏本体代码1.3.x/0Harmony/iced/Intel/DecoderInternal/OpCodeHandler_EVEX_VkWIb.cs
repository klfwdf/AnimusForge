using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006CF RID: 1743
	internal sealed class OpCodeHandler_EVEX_VkWIb : OpCodeHandlerModRM
	{
		// Token: 0x06002480 RID: 9344 RVA: 0x0007890E File Offset: 0x00076B0E
		public OpCodeHandler_EVEX_VkWIb(Register baseReg, Code code, TupleType tupleType, bool canBroadcast)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x06002481 RID: 9345 RVA: 0x0007893C File Offset: 0x00076B3C
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
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040036DC RID: 14044
		private readonly Register baseReg1;

		// Token: 0x040036DD RID: 14045
		private readonly Register baseReg2;

		// Token: 0x040036DE RID: 14046
		private readonly Code code;

		// Token: 0x040036DF RID: 14047
		private readonly TupleType tupleType;

		// Token: 0x040036E0 RID: 14048
		private readonly bool canBroadcast;
	}
}
