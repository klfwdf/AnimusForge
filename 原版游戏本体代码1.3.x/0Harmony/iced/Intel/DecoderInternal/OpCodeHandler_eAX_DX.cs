using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000767 RID: 1895
	internal sealed class OpCodeHandler_eAX_DX : OpCodeHandler
	{
		// Token: 0x060025B8 RID: 9656 RVA: 0x0007F882 File Offset: 0x0007DA82
		public OpCodeHandler_eAX_DX(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x060025B9 RID: 9657 RVA: 0x0007F898 File Offset: 0x0007DA98
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.Op1Register = Register.DX;
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Register = Register.EAX;
				return;
			}
			instruction.InternalSetCodeNoCheck(this.code16);
			instruction.Op0Register = Register.AX;
		}

		// Token: 0x04003831 RID: 14385
		private readonly Code code16;

		// Token: 0x04003832 RID: 14386
		private readonly Code code32;
	}
}
