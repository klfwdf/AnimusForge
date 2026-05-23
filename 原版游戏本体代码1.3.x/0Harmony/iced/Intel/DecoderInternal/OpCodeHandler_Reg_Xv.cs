using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000750 RID: 1872
	internal sealed class OpCodeHandler_Reg_Xv : OpCodeHandler
	{
		// Token: 0x0600258A RID: 9610 RVA: 0x0007EAE3 File Offset: 0x0007CCE3
		public OpCodeHandler_Reg_Xv(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x0600258B RID: 9611 RVA: 0x0007EAFC File Offset: 0x0007CCFC
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op0Register = ((int)operandSize << 4) + Register.AX;
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.Op1Kind = OpKind.MemorySegRSI;
				return;
			}
			if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.Op1Kind = OpKind.MemorySegESI;
				return;
			}
			instruction.Op1Kind = OpKind.MemorySegSI;
		}

		// Token: 0x0400380D RID: 14349
		private readonly Code3 codes;
	}
}
