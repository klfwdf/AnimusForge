using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007B5 RID: 1973
	internal sealed class OpCodeHandler_VEX_VHWIs4 : OpCodeHandlerModRM
	{
		// Token: 0x06002660 RID: 9824 RVA: 0x00082FAB File Offset: 0x000811AB
		public OpCodeHandler_VEX_VHWIs4(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x06002661 RID: 9825 RVA: 0x00082FC4 File Offset: 0x000811C4
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
			instruction.Op3Register = (int)((decoder.ReadByte() >> 4) & decoder.reg15Mask) + this.baseReg;
		}

		// Token: 0x040038BF RID: 14527
		private readonly Register baseReg;

		// Token: 0x040038C0 RID: 14528
		private readonly Code code;
	}
}
