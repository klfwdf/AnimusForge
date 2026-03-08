using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006D2 RID: 1746
	internal sealed class OpCodeHandler_EVEX_HWIb : OpCodeHandlerModRM
	{
		// Token: 0x06002486 RID: 9350 RVA: 0x00078C86 File Offset: 0x00076E86
		public OpCodeHandler_EVEX_HWIb(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x06002487 RID: 9351 RVA: 0x00078CAC File Offset: 0x00076EAC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
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
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040036EA RID: 14058
		private readonly Register baseReg1;

		// Token: 0x040036EB RID: 14059
		private readonly Register baseReg2;

		// Token: 0x040036EC RID: 14060
		private readonly Code code;

		// Token: 0x040036ED RID: 14061
		private readonly TupleType tupleType;
	}
}
