using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000757 RID: 1879
	internal sealed class OpCodeHandler_Xv_Yv : OpCodeHandler
	{
		// Token: 0x06002598 RID: 9624 RVA: 0x0007EE9F File Offset: 0x0007D09F
		public OpCodeHandler_Xv_Yv(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002599 RID: 9625 RVA: 0x0007EEB8 File Offset: 0x0007D0B8
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.Op0Kind = OpKind.MemorySegRSI;
				instruction.Op1Kind = OpKind.MemoryESRDI;
				return;
			}
			if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.Op0Kind = OpKind.MemorySegESI;
				instruction.Op1Kind = OpKind.MemoryESEDI;
				return;
			}
			instruction.Op0Kind = OpKind.MemorySegSI;
			instruction.Op1Kind = OpKind.MemoryESDI;
		}

		// Token: 0x04003816 RID: 14358
		private readonly Code3 codes;
	}
}
