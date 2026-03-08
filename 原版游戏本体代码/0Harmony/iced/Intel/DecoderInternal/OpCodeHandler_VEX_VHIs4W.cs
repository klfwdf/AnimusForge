using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007B6 RID: 1974
	internal sealed class OpCodeHandler_VEX_VHIs4W : OpCodeHandlerModRM
	{
		// Token: 0x06002662 RID: 9826 RVA: 0x00083083 File Offset: 0x00081283
		public OpCodeHandler_VEX_VHIs4W(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x06002663 RID: 9827 RVA: 0x0008309C File Offset: 0x0008129C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op3Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg;
			}
			else
			{
				instruction.Op3Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			instruction.Op2Register = (int)((decoder.ReadByte() >> 4) & decoder.reg15Mask) + this.baseReg;
		}

		// Token: 0x040038C1 RID: 14529
		private readonly Register baseReg;

		// Token: 0x040038C2 RID: 14530
		private readonly Code code;
	}
}
