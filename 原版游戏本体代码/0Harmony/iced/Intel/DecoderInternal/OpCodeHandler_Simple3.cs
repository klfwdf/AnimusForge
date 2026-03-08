using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000739 RID: 1849
	internal sealed class OpCodeHandler_Simple3 : OpCodeHandler
	{
		// Token: 0x0600255A RID: 9562 RVA: 0x0007DB27 File Offset: 0x0007BD27
		public OpCodeHandler_Simple3(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600255B RID: 9563 RVA: 0x0007DB44 File Offset: 0x0007BD44
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

		// Token: 0x040037DC RID: 14300
		private readonly Code code16;

		// Token: 0x040037DD RID: 14301
		private readonly Code code32;

		// Token: 0x040037DE RID: 14302
		private readonly Code code64;
	}
}
