using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200075C RID: 1884
	internal sealed class OpCodeHandler_Sw_M : OpCodeHandlerModRM
	{
		// Token: 0x060025A2 RID: 9634 RVA: 0x0007F18A File Offset: 0x0007D38A
		public OpCodeHandler_Sw_M(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025A3 RID: 9635 RVA: 0x0007F199 File Offset: 0x0007D399
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = decoder.ReadOpSegReg();
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400381B RID: 14363
		private readonly Code code;
	}
}
