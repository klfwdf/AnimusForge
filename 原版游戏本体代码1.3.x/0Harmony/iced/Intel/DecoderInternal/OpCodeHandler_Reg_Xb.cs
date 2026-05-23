using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200074F RID: 1871
	internal sealed class OpCodeHandler_Reg_Xb : OpCodeHandler
	{
		// Token: 0x06002588 RID: 9608 RVA: 0x0007EA72 File Offset: 0x0007CC72
		public OpCodeHandler_Reg_Xb(Code code, Register reg)
		{
			this.code = code;
			this.reg = reg;
		}

		// Token: 0x06002589 RID: 9609 RVA: 0x0007EA88 File Offset: 0x0007CC88
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = this.reg;
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

		// Token: 0x0400380B RID: 14347
		private readonly Code code;

		// Token: 0x0400380C RID: 14348
		private readonly Register reg;
	}
}
