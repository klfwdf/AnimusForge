using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200073B RID: 1851
	internal sealed class OpCodeHandler_Simple5_a32 : OpCodeHandler
	{
		// Token: 0x0600255E RID: 9566 RVA: 0x0007DBF8 File Offset: 0x0007BDF8
		public OpCodeHandler_Simple5_a32(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x0600255F RID: 9567 RVA: 0x0007DC10 File Offset: 0x0007BE10
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.addressSize != OpSize.Size32 && decoder.invalidCheckMask != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			UIntPtr addressSize = (UIntPtr)decoder.state.addressSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)addressSize * 2UL))));
		}

		// Token: 0x040037E0 RID: 14304
		private readonly Code3 codes;
	}
}
