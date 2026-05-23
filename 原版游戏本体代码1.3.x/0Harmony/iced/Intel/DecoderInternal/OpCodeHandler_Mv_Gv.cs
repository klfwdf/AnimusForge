using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000797 RID: 1943
	internal sealed class OpCodeHandler_Mv_Gv : OpCodeHandlerModRM
	{
		// Token: 0x0600261F RID: 9759 RVA: 0x00081917 File Offset: 0x0007FB17
		public OpCodeHandler_Mv_Gv(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002620 RID: 9760 RVA: 0x00081930 File Offset: 0x0007FB30
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op1Register = ((int)operandSize << 4) + (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400387D RID: 14461
		private readonly Code3 codes;
	}
}
