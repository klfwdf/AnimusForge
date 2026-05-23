using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000774 RID: 1908
	internal sealed class OpCodeHandler_rDI_VX_RX : OpCodeHandlerModRM
	{
		// Token: 0x060025D8 RID: 9688 RVA: 0x0008010A File Offset: 0x0007E30A
		public OpCodeHandler_rDI_VX_RX(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025D9 RID: 9689 RVA: 0x0008011C File Offset: 0x0007E31C
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
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x04003848 RID: 14408
		private readonly Code code;
	}
}
