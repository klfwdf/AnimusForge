using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007B8 RID: 1976
	internal sealed class OpCodeHandler_VEX_VHIs5W : OpCodeHandlerModRM
	{
		// Token: 0x06002666 RID: 9830 RVA: 0x0008323F File Offset: 0x0008143F
		public OpCodeHandler_VEX_VHIs5W(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x06002667 RID: 9831 RVA: 0x00083258 File Offset: 0x00081458
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
			uint ib = decoder.ReadByte();
			instruction.Op2Register = (int)((ib >> 4) & decoder.reg15Mask) + this.baseReg;
			instruction.InternalImmediate8 = ib & 15U;
		}

		// Token: 0x040038C5 RID: 14533
		private readonly Register baseReg;

		// Token: 0x040038C6 RID: 14534
		private readonly Code code;
	}
}
