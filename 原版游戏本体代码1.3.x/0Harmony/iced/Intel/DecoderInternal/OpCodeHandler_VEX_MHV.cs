using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007B2 RID: 1970
	internal sealed class OpCodeHandler_VEX_MHV : OpCodeHandlerModRM
	{
		// Token: 0x06002659 RID: 9817 RVA: 0x00082D7B File Offset: 0x00080F7B
		public OpCodeHandler_VEX_MHV(Register baseReg, Code code)
		{
			this.baseReg = baseReg;
			this.code = code;
		}

		// Token: 0x0600265A RID: 9818 RVA: 0x00082D94 File Offset: 0x00080F94
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			instruction.Op2Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038B7 RID: 14519
		private readonly Register baseReg;

		// Token: 0x040038B8 RID: 14520
		private readonly Code code;
	}
}
