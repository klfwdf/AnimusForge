using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000736 RID: 1846
	internal sealed class OpCodeHandler_PushSimple2 : OpCodeHandler
	{
		// Token: 0x06002554 RID: 9556 RVA: 0x0007D9F2 File Offset: 0x0007BBF2
		public OpCodeHandler_PushSimple2(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002555 RID: 9557 RVA: 0x0007DA10 File Offset: 0x0007BC10
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if (decoder.state.operandSize != OpSize.Size16)
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

		// Token: 0x040037D7 RID: 14295
		private readonly Code code16;

		// Token: 0x040037D8 RID: 14296
		private readonly Code code32;

		// Token: 0x040037D9 RID: 14297
		private readonly Code code64;
	}
}
