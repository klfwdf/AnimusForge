using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000755 RID: 1877
	internal sealed class OpCodeHandler_Yv_Xv : OpCodeHandler
	{
		// Token: 0x06002594 RID: 9620 RVA: 0x0007ED87 File Offset: 0x0007CF87
		public OpCodeHandler_Yv_Xv(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002595 RID: 9621 RVA: 0x0007EDA0 File Offset: 0x0007CFA0
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.Op0Kind = OpKind.MemoryESRDI;
				instruction.Op1Kind = OpKind.MemorySegRSI;
				return;
			}
			if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.Op0Kind = OpKind.MemoryESEDI;
				instruction.Op1Kind = OpKind.MemorySegESI;
				return;
			}
			instruction.Op0Kind = OpKind.MemoryESDI;
			instruction.Op1Kind = OpKind.MemorySegSI;
		}

		// Token: 0x04003814 RID: 14356
		private readonly Code3 codes;
	}
}
