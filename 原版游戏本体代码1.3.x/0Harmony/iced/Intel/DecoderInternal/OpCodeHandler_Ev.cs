using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000716 RID: 1814
	internal sealed class OpCodeHandler_Ev : OpCodeHandlerModRM
	{
		// Token: 0x06002512 RID: 9490 RVA: 0x0007BED0 File Offset: 0x0007A0D0
		public OpCodeHandler_Ev(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002513 RID: 9491 RVA: 0x0007BEE6 File Offset: 0x0007A0E6
		public OpCodeHandler_Ev(Code code16, Code code32, Code code64, HandlerFlags flags)
		{
			this.codes = new Code3(code16, code32, code64);
			this.flags = flags;
		}

		// Token: 0x06002514 RID: 9492 RVA: 0x0007BF04 File Offset: 0x0007A104
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
				return;
			}
			decoder.state.zs.flags = decoder.state.zs.flags | (StateFlags)((this.flags & HandlerFlags.Lock) << 10);
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003791 RID: 14225
		private readonly Code3 codes;

		// Token: 0x04003792 RID: 14226
		private readonly HandlerFlags flags;
	}
}
