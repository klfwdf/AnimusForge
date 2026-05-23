using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000704 RID: 1796
	internal sealed class OpCodeHandler_RegIb : OpCodeHandler
	{
		// Token: 0x060024EC RID: 9452 RVA: 0x0007B468 File Offset: 0x00079668
		public OpCodeHandler_RegIb(Code code, Register reg)
		{
			this.code = code;
			this.reg = reg;
		}

		// Token: 0x060024ED RID: 9453 RVA: 0x0007B47E File Offset: 0x0007967E
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = this.reg;
			instruction.Op1Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x04003773 RID: 14195
		private readonly Code code;

		// Token: 0x04003774 RID: 14196
		private readonly Register reg;
	}
}
