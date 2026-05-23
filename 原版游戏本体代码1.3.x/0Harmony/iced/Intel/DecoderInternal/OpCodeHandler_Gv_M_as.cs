using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000722 RID: 1826
	internal sealed class OpCodeHandler_Gv_M_as : OpCodeHandlerModRM
	{
		// Token: 0x0600252B RID: 9515 RVA: 0x0007C728 File Offset: 0x0007A928
		public OpCodeHandler_Gv_M_as(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x0600252C RID: 9516 RVA: 0x0007C740 File Offset: 0x0007A940
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr addressSize = (UIntPtr)decoder.state.addressSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)addressSize * 2UL))));
			instruction.Op0Register = ((int)addressSize << 4) + (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040037A9 RID: 14249
		private readonly Code3 codes;
	}
}
