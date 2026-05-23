using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000779 RID: 1913
	internal sealed class OpCodeHandler_P_Q : OpCodeHandlerModRM
	{
		// Token: 0x060025E2 RID: 9698 RVA: 0x000803DC File Offset: 0x0007E5DC
		public OpCodeHandler_P_Q(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025E3 RID: 9699 RVA: 0x000803EC File Offset: 0x0007E5EC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.MM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)decoder.state.rm + Register.MM0;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400384D RID: 14413
		private readonly Code code;
	}
}
