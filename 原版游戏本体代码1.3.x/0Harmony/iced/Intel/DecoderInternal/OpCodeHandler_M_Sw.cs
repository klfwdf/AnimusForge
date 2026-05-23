using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000759 RID: 1881
	internal sealed class OpCodeHandler_M_Sw : OpCodeHandlerModRM
	{
		// Token: 0x0600259C RID: 9628 RVA: 0x0007EFE1 File Offset: 0x0007D1E1
		public OpCodeHandler_M_Sw(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600259D RID: 9629 RVA: 0x0007EFF0 File Offset: 0x0007D1F0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = decoder.ReadOpSegReg();
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003818 RID: 14360
		private readonly Code code;
	}
}
