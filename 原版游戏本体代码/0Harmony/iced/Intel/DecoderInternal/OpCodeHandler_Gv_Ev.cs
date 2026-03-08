using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000720 RID: 1824
	internal sealed class OpCodeHandler_Gv_Ev : OpCodeHandlerModRM
	{
		// Token: 0x06002527 RID: 9511 RVA: 0x0007C5DB File Offset: 0x0007A7DB
		public OpCodeHandler_Gv_Ev(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002528 RID: 9512 RVA: 0x0007C5F4 File Offset: 0x0007A7F4
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
			if (decoder.state.mod < 3U)
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
				return;
			}
			instruction.Op1Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
		}

		// Token: 0x040037A7 RID: 14247
		private readonly Code3 codes;
	}
}
