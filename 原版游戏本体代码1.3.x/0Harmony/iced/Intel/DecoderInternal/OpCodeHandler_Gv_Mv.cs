using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000796 RID: 1942
	internal sealed class OpCodeHandler_Gv_Mv : OpCodeHandlerModRM
	{
		// Token: 0x0600261D RID: 9757 RVA: 0x0008187A File Offset: 0x0007FA7A
		public OpCodeHandler_Gv_Mv(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x0600261E RID: 9758 RVA: 0x00081890 File Offset: 0x0007FA90
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400387C RID: 14460
		private readonly Code3 codes;
	}
}
