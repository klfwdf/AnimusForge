using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007CD RID: 1997
	internal sealed class OpCodeHandler_VEX_SIBMEM_VT : OpCodeHandlerModRM
	{
		// Token: 0x06002690 RID: 9872 RVA: 0x000842EF File Offset: 0x000824EF
		public OpCodeHandler_VEX_SIBMEM_VT(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002691 RID: 9873 RVA: 0x00084300 File Offset: 0x00082500
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (((decoder.state.vvvv_invalidCheck | decoder.state.zs.extraRegisterBase) & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)decoder.state.reg + Register.TMM0;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMemSib(ref instruction);
		}

		// Token: 0x040038EF RID: 14575
		private readonly Code code;
	}
}
