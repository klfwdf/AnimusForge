using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000753 RID: 1875
	internal sealed class OpCodeHandler_Reg_Yv : OpCodeHandler
	{
		// Token: 0x06002590 RID: 9616 RVA: 0x0007EC7B File Offset: 0x0007CE7B
		public OpCodeHandler_Reg_Yv(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002591 RID: 9617 RVA: 0x0007EC94 File Offset: 0x0007CE94
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op0Register = ((int)operandSize << 4) + Register.AX;
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.Op1Kind = OpKind.MemoryESRDI;
				return;
			}
			if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.Op1Kind = OpKind.MemoryESEDI;
				return;
			}
			instruction.Op1Kind = OpKind.MemoryESDI;
		}

		// Token: 0x04003812 RID: 14354
		private readonly Code3 codes;
	}
}
