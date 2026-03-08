using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000776 RID: 1910
	internal sealed class OpCodeHandler_VM : OpCodeHandlerModRM
	{
		// Token: 0x060025DC RID: 9692 RVA: 0x00080270 File Offset: 0x0007E470
		public OpCodeHandler_VM(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025DD RID: 9693 RVA: 0x00080280 File Offset: 0x0007E480
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400384A RID: 14410
		private readonly Code code;
	}
}
