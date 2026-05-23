using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200074D RID: 1869
	internal sealed class OpCodeHandler_Yv_Reg : OpCodeHandler
	{
		// Token: 0x06002584 RID: 9604 RVA: 0x0007E94B File Offset: 0x0007CB4B
		public OpCodeHandler_Yv_Reg(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002585 RID: 9605 RVA: 0x0007E964 File Offset: 0x0007CB64
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op1Register = ((int)operandSize << 4) + Register.AX;
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.Op0Kind = OpKind.MemoryESRDI;
				return;
			}
			if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.Op0Kind = OpKind.MemoryESEDI;
				return;
			}
			instruction.Op0Kind = OpKind.MemoryESDI;
		}

		// Token: 0x04003808 RID: 14344
		private readonly Code3 codes;
	}
}
