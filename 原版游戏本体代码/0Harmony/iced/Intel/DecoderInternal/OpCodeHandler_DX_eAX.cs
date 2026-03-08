using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000768 RID: 1896
	internal sealed class OpCodeHandler_DX_eAX : OpCodeHandler
	{
		// Token: 0x060025BA RID: 9658 RVA: 0x0007F8D8 File Offset: 0x0007DAD8
		public OpCodeHandler_DX_eAX(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x060025BB RID: 9659 RVA: 0x0007F8EE File Offset: 0x0007DAEE
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.Op0Register = Register.DX;
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op1Register = Register.EAX;
				return;
			}
			instruction.InternalSetCodeNoCheck(this.code16);
			instruction.Op1Register = Register.AX;
		}

		// Token: 0x04003833 RID: 14387
		private readonly Code code16;

		// Token: 0x04003834 RID: 14388
		private readonly Code code32;
	}
}
