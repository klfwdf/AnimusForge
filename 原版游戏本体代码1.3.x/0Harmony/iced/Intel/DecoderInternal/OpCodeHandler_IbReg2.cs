using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000766 RID: 1894
	internal sealed class OpCodeHandler_IbReg2 : OpCodeHandler
	{
		// Token: 0x060025B6 RID: 9654 RVA: 0x0007F816 File Offset: 0x0007DA16
		public OpCodeHandler_IbReg2(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x060025B7 RID: 9655 RVA: 0x0007F82C File Offset: 0x0007DA2C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.Op0Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op1Register = Register.EAX;
				return;
			}
			instruction.InternalSetCodeNoCheck(this.code16);
			instruction.Op1Register = Register.AX;
		}

		// Token: 0x0400382F RID: 14383
		private readonly Code code16;

		// Token: 0x04003830 RID: 14384
		private readonly Code code32;
	}
}
