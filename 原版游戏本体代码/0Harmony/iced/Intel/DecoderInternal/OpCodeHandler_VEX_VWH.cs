using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007AF RID: 1967
	internal sealed class OpCodeHandler_VEX_VWH : OpCodeHandlerModRM
	{
		// Token: 0x06002653 RID: 9811 RVA: 0x00082B92 File Offset: 0x00080D92
		public OpCodeHandler_VEX_VWH(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x06002654 RID: 9812 RVA: 0x00082BA8 File Offset: 0x00080DA8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
			instruction.Op2Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038B1 RID: 14513
		private readonly Register baseReg;

		// Token: 0x040038B2 RID: 14514
		private readonly Code code;
	}
}
