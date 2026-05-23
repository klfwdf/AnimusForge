using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007B7 RID: 1975
	internal sealed class OpCodeHandler_VEX_VHWIs5 : OpCodeHandlerModRM
	{
		// Token: 0x06002664 RID: 9828 RVA: 0x0008315B File Offset: 0x0008135B
		public OpCodeHandler_VEX_VHWIs5(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x06002665 RID: 9829 RVA: 0x00083174 File Offset: 0x00081374
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg;
			}
			else
			{
				instruction.Op2Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			uint ib = decoder.ReadByte();
			instruction.Op3Register = (int)((ib >> 4) & decoder.reg15Mask) + this.baseReg;
			instruction.InternalImmediate8 = ib & 15U;
		}

		// Token: 0x040038C3 RID: 14531
		private readonly Register baseReg;

		// Token: 0x040038C4 RID: 14532
		private readonly Code code;
	}
}
