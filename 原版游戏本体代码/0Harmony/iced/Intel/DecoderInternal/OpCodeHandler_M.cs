using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200076F RID: 1903
	internal sealed class OpCodeHandler_M : OpCodeHandlerModRM
	{
		// Token: 0x060025CB RID: 9675 RVA: 0x0007FDD0 File Offset: 0x0007DFD0
		public OpCodeHandler_M(Code codeW0, Code codeW1)
		{
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
		}

		// Token: 0x060025CC RID: 9676 RVA: 0x0007FDE6 File Offset: 0x0007DFE6
		public OpCodeHandler_M(Code codeW0)
		{
			this.codeW0 = codeW0;
			this.codeW1 = codeW0;
		}

		// Token: 0x060025CD RID: 9677 RVA: 0x0007FDFC File Offset: 0x0007DFFC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.codeW1);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.codeW0);
			}
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400383E RID: 14398
		private readonly Code codeW0;

		// Token: 0x0400383F RID: 14399
		private readonly Code codeW1;
	}
}
