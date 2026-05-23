using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000775 RID: 1909
	internal sealed class OpCodeHandler_rDI_P_N : OpCodeHandlerModRM
	{
		// Token: 0x060025DA RID: 9690 RVA: 0x000801CC File Offset: 0x0007E3CC
		public OpCodeHandler_rDI_P_N(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025DB RID: 9691 RVA: 0x000801DC File Offset: 0x0007E3DC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.Op0Kind = OpKind.MemorySegRDI;
			}
			else if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.Op0Kind = OpKind.MemorySegEDI;
			}
			else
			{
				instruction.Op0Kind = OpKind.MemorySegDI;
			}
			instruction.Op1Register = (int)decoder.state.reg + Register.MM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)decoder.state.rm + Register.MM0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x04003849 RID: 14409
		private readonly Code code;
	}
}
