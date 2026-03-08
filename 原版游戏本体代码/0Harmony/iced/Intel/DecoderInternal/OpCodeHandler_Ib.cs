using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000708 RID: 1800
	internal sealed class OpCodeHandler_Ib : OpCodeHandler
	{
		// Token: 0x060024F4 RID: 9460 RVA: 0x0007B546 File Offset: 0x00079746
		public OpCodeHandler_Ib(Code code)
		{
			this.code = code;
		}

		// Token: 0x060024F5 RID: 9461 RVA: 0x0007B555 File Offset: 0x00079755
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x04003779 RID: 14201
		private readonly Code code;
	}
}
