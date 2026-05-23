using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200073C RID: 1852
	internal sealed class OpCodeHandler_Simple5_ModRM_as : OpCodeHandlerModRM
	{
		// Token: 0x06002560 RID: 9568 RVA: 0x0007DC64 File Offset: 0x0007BE64
		public OpCodeHandler_Simple5_ModRM_as(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002561 RID: 9569 RVA: 0x0007DC7C File Offset: 0x0007BE7C
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr addressSize = (UIntPtr)decoder.state.addressSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)addressSize * 2UL))));
			instruction.Op0Register = ((int)addressSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
		}

		// Token: 0x040037E1 RID: 14305
		private readonly Code3 codes;
	}
}
