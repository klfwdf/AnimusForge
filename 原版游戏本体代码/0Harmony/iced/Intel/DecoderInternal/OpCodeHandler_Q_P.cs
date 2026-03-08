using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200077A RID: 1914
	internal sealed class OpCodeHandler_Q_P : OpCodeHandlerModRM
	{
		// Token: 0x060025E4 RID: 9700 RVA: 0x00080452 File Offset: 0x0007E652
		public OpCodeHandler_Q_P(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025E5 RID: 9701 RVA: 0x00080464 File Offset: 0x0007E664
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)decoder.state.reg + Register.MM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)decoder.state.rm + Register.MM0;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400384E RID: 14414
		private readonly Code code;
	}
}
