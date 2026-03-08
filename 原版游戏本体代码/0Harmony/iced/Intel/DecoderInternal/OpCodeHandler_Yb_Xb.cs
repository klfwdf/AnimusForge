using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000754 RID: 1876
	internal sealed class OpCodeHandler_Yb_Xb : OpCodeHandler
	{
		// Token: 0x06002592 RID: 9618 RVA: 0x0007ED0F File Offset: 0x0007CF0F
		public OpCodeHandler_Yb_Xb(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002593 RID: 9619 RVA: 0x0007ED20 File Offset: 0x0007CF20
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
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

		// Token: 0x04003813 RID: 14355
		private readonly Code code;
	}
}
