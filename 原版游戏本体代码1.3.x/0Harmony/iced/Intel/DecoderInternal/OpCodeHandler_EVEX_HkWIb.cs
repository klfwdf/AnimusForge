using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006D1 RID: 1745
	internal sealed class OpCodeHandler_EVEX_HkWIb : OpCodeHandlerModRM
	{
		// Token: 0x06002484 RID: 9348 RVA: 0x00078B76 File Offset: 0x00076D76
		public OpCodeHandler_EVEX_HkWIb(Register baseReg, Code code, TupleType tupleType, bool canBroadcast)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.canBroadcast = canBroadcast;
		}

		// Token: 0x06002485 RID: 9349 RVA: 0x00078BA4 File Offset: 0x00076DA4
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.vvvv + this.baseReg1;
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

		// Token: 0x040036E5 RID: 14053
		private readonly Register baseReg1;

		// Token: 0x040036E6 RID: 14054
		private readonly Register baseReg2;

		// Token: 0x040036E7 RID: 14055
		private readonly Code code;

		// Token: 0x040036E8 RID: 14056
		private readonly TupleType tupleType;

		// Token: 0x040036E9 RID: 14057
		private readonly bool canBroadcast;
	}
}
