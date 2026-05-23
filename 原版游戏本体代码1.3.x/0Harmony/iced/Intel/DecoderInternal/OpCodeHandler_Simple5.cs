using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200073A RID: 1850
	internal sealed class OpCodeHandler_Simple5 : OpCodeHandler
	{
		// Token: 0x0600255C RID: 9564 RVA: 0x0007DBA7 File Offset: 0x0007BDA7
		public OpCodeHandler_Simple5(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x0600255D RID: 9565 RVA: 0x0007DBC0 File Offset: 0x0007BDC0
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr addressSize = (UIntPtr)decoder.state.addressSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)addressSize * 2UL))));
		}

		// Token: 0x040037DF RID: 14303
		private readonly Code3 codes;
	}
}
