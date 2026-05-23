using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007AA RID: 1962
	internal sealed class OpCodeHandler_VEX_rDI_VX_RX : OpCodeHandlerModRM
	{
		// Token: 0x06002646 RID: 9798 RVA: 0x00082678 File Offset: 0x00080878
		public OpCodeHandler_VEX_rDI_VX_RX(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x06002647 RID: 9799 RVA: 0x00082690 File Offset: 0x00080890
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
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
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg;
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x040038A0 RID: 14496
		private readonly Register baseReg;

		// Token: 0x040038A1 RID: 14497
		private readonly Code code;
	}
}
