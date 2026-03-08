using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000703 RID: 1795
	internal sealed class OpCodeHandler_Reg : OpCodeHandler
	{
		// Token: 0x060024EA RID: 9450 RVA: 0x0007B438 File Offset: 0x00079638
		public OpCodeHandler_Reg(Code code, Register reg)
		{
			this.code = code;
			this.reg = reg;
		}

		// Token: 0x060024EB RID: 9451 RVA: 0x0007B44E File Offset: 0x0007964E
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = this.reg;
		}

		// Token: 0x04003771 RID: 14193
		private readonly Code code;

		// Token: 0x04003772 RID: 14194
		private readonly Register reg;
	}
}
