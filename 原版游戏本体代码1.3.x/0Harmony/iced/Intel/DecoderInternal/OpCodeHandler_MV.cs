using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000777 RID: 1911
	internal sealed class OpCodeHandler_MV : OpCodeHandlerModRM
	{
		// Token: 0x060025DE RID: 9694 RVA: 0x000802E3 File Offset: 0x0007E4E3
		public OpCodeHandler_MV(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025DF RID: 9695 RVA: 0x000802F4 File Offset: 0x0007E4F4
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400384B RID: 14411
		private readonly Code code;
	}
}
