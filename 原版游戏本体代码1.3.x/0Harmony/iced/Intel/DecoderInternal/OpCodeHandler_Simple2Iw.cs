using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000738 RID: 1848
	internal sealed class OpCodeHandler_Simple2Iw : OpCodeHandler
	{
		// Token: 0x06002558 RID: 9560 RVA: 0x0007DAC4 File Offset: 0x0007BCC4
		public OpCodeHandler_Simple2Iw(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002559 RID: 9561 RVA: 0x0007DADC File Offset: 0x0007BCDC
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op0Kind = OpKind.Immediate16;
			instruction.InternalImmediate16 = decoder.ReadUInt16();
		}

		// Token: 0x040037DB RID: 14299
		private readonly Code3 codes;
	}
}
