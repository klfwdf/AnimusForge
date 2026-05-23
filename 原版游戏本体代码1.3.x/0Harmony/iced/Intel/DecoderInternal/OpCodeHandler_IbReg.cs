using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000705 RID: 1797
	internal sealed class OpCodeHandler_IbReg : OpCodeHandler
	{
		// Token: 0x060024EE RID: 9454 RVA: 0x0007B4AB File Offset: 0x000796AB
		public OpCodeHandler_IbReg(Code code, Register reg)
		{
			this.code = code;
			this.reg = reg;
		}

		// Token: 0x060024EF RID: 9455 RVA: 0x0007B4C1 File Offset: 0x000796C1
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = this.reg;
			instruction.Op0Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x04003775 RID: 14197
		private readonly Code code;

		// Token: 0x04003776 RID: 14198
		private readonly Register reg;
	}
}
