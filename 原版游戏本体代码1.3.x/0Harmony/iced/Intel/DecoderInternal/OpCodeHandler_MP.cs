using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200077B RID: 1915
	internal sealed class OpCodeHandler_MP : OpCodeHandlerModRM
	{
		// Token: 0x060025E6 RID: 9702 RVA: 0x000804CA File Offset: 0x0007E6CA
		public OpCodeHandler_MP(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025E7 RID: 9703 RVA: 0x000804DC File Offset: 0x0007E6DC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)decoder.state.reg + Register.MM0;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400384F RID: 14415
		private readonly Code code;
	}
}
