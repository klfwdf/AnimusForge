using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000737 RID: 1847
	internal sealed class OpCodeHandler_Simple2 : OpCodeHandler
	{
		// Token: 0x06002556 RID: 9558 RVA: 0x0007DA73 File Offset: 0x0007BC73
		public OpCodeHandler_Simple2(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002557 RID: 9559 RVA: 0x0007DA8C File Offset: 0x0007BC8C
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
		}

		// Token: 0x040037DA RID: 14298
		private readonly Code3 codes;
	}
}
