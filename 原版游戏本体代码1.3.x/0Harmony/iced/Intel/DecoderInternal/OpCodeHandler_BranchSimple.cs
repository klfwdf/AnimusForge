using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000763 RID: 1891
	internal sealed class OpCodeHandler_BranchSimple : OpCodeHandler
	{
		// Token: 0x060025B0 RID: 9648 RVA: 0x0007F677 File Offset: 0x0007D877
		public OpCodeHandler_BranchSimple(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x060025B1 RID: 9649 RVA: 0x0007F694 File Offset: 0x0007D894
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if ((decoder.options & DecoderOptions.AMD) == DecoderOptions.None || decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				return;
			}
			else
			{
				if (decoder.state.operandSize == OpSize.Size32)
				{
					instruction.InternalSetCodeNoCheck(this.code32);
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				return;
			}
		}

		// Token: 0x04003827 RID: 14375
		private readonly Code code16;

		// Token: 0x04003828 RID: 14376
		private readonly Code code32;

		// Token: 0x04003829 RID: 14377
		private readonly Code code64;
	}
}
