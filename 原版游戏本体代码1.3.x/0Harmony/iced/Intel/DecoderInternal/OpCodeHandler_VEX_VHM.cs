using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007B1 RID: 1969
	internal sealed class OpCodeHandler_VEX_VHM : OpCodeHandlerModRM
	{
		// Token: 0x06002657 RID: 9815 RVA: 0x00082CE3 File Offset: 0x00080EE3
		public OpCodeHandler_VEX_VHM(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x06002658 RID: 9816 RVA: 0x00082CFC File Offset: 0x00080EFC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op2Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038B5 RID: 14517
		private readonly Register baseReg;

		// Token: 0x040038B6 RID: 14518
		private readonly Code code;
	}
}
