using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000714 RID: 1812
	internal sealed class OpCodeHandler_Ev_1 : OpCodeHandlerModRM
	{
		// Token: 0x0600250E RID: 9486 RVA: 0x0007BD70 File Offset: 0x00079F70
		public OpCodeHandler_Ev_1(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x0600250F RID: 9487 RVA: 0x0007BD88 File Offset: 0x00079F88
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op1Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = 1U;
			decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.NoImm;
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400378F RID: 14223
		private readonly Code3 codes;
	}
}
