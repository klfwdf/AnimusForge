using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000752 RID: 1874
	internal sealed class OpCodeHandler_Reg_Yb : OpCodeHandler
	{
		// Token: 0x0600258E RID: 9614 RVA: 0x0007EC0A File Offset: 0x0007CE0A
		public OpCodeHandler_Reg_Yb(Code code, Register reg)
		{
			this.code = code;
			this.reg = reg;
		}

		// Token: 0x0600258F RID: 9615 RVA: 0x0007EC20 File Offset: 0x0007CE20
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = this.reg;
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

		// Token: 0x04003810 RID: 14352
		private readonly Code code;

		// Token: 0x04003811 RID: 14353
		private readonly Register reg;
	}
}
