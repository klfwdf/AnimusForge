using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200072D RID: 1837
	internal sealed class OpCodeHandler_PushOpSizeReg : OpCodeHandler
	{
		// Token: 0x06002541 RID: 9537 RVA: 0x0007D255 File Offset: 0x0007B455
		public OpCodeHandler_PushOpSizeReg(Code code16, Code code32, Code code64, Register reg)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
			this.reg = reg;
		}

		// Token: 0x06002542 RID: 9538 RVA: 0x0007D27C File Offset: 0x0007B47C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if (decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
				}
				else
				{
					instruction.InternalSetCodeNoCheck(this.code16);
				}
			}
			else if (decoder.state.operandSize == OpSize.Size32)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code16);
			}
			instruction.Op0Register = this.reg;
		}

		// Token: 0x040037C5 RID: 14277
		private readonly Code code16;

		// Token: 0x040037C6 RID: 14278
		private readonly Code code32;

		// Token: 0x040037C7 RID: 14279
		private readonly Code code64;

		// Token: 0x040037C8 RID: 14280
		private readonly Register reg;
	}
}
