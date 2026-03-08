using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000765 RID: 1893
	internal sealed class OpCodeHandler_Reg_Ib2 : OpCodeHandler
	{
		// Token: 0x060025B4 RID: 9652 RVA: 0x0007F7A9 File Offset: 0x0007D9A9
		public OpCodeHandler_Reg_Ib2(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x060025B5 RID: 9653 RVA: 0x0007F7C0 File Offset: 0x0007D9C0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.Op1Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Register = Register.EAX;
				return;
			}
			instruction.InternalSetCodeNoCheck(this.code16);
			instruction.Op0Register = Register.AX;
		}

		// Token: 0x0400382D RID: 14381
		private readonly Code code16;

		// Token: 0x0400382E RID: 14382
		private readonly Code code32;
	}
}
