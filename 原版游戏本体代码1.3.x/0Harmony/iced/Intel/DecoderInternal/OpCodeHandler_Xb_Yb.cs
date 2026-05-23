using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000756 RID: 1878
	internal sealed class OpCodeHandler_Xb_Yb : OpCodeHandler
	{
		// Token: 0x06002596 RID: 9622 RVA: 0x0007EE26 File Offset: 0x0007D026
		public OpCodeHandler_Xb_Yb(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002597 RID: 9623 RVA: 0x0007EE38 File Offset: 0x0007D038
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
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

		// Token: 0x04003815 RID: 14357
		private readonly Code code;
	}
}
