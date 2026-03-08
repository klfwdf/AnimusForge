using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200074C RID: 1868
	internal sealed class OpCodeHandler_Yb_Reg : OpCodeHandler
	{
		// Token: 0x06002582 RID: 9602 RVA: 0x0007E8D7 File Offset: 0x0007CAD7
		public OpCodeHandler_Yb_Reg(Code code, Register reg)
		{
			this.code = code;
			this.reg = reg;
		}

		// Token: 0x06002583 RID: 9603 RVA: 0x0007E8F0 File Offset: 0x0007CAF0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = this.reg;
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

		// Token: 0x04003806 RID: 14342
		private readonly Code code;

		// Token: 0x04003807 RID: 14343
		private readonly Register reg;
	}
}
