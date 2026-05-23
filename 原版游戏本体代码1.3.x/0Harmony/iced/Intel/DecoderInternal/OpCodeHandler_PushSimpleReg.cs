using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200073E RID: 1854
	internal sealed class OpCodeHandler_PushSimpleReg : OpCodeHandler
	{
		// Token: 0x06002564 RID: 9572 RVA: 0x0007DD27 File Offset: 0x0007BF27
		public OpCodeHandler_PushSimpleReg(int index, Code code16, Code code32, Code code64)
		{
			this.index = index;
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002565 RID: 9573 RVA: 0x0007DD4C File Offset: 0x0007BF4C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if (decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
					instruction.Op0Register = this.index + (int)decoder.state.zs.extraBaseRegisterBase + Register.RAX;
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Register = this.index + (int)decoder.state.zs.extraBaseRegisterBase + Register.AX;
				return;
			}
			else
			{
				if (decoder.state.operandSize == OpSize.Size32)
				{
					instruction.InternalSetCodeNoCheck(this.code32);
					instruction.Op0Register = this.index + (int)decoder.state.zs.extraBaseRegisterBase + Register.EAX;
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Register = this.index + (int)decoder.state.zs.extraBaseRegisterBase + Register.AX;
				return;
			}
		}

		// Token: 0x040037E4 RID: 14308
		private readonly int index;

		// Token: 0x040037E5 RID: 14309
		private readonly Code code16;

		// Token: 0x040037E6 RID: 14310
		private readonly Code code32;

		// Token: 0x040037E7 RID: 14311
		private readonly Code code64;
	}
}
