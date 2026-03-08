using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007CC RID: 1996
	internal sealed class OpCodeHandler_VEX_VT_SIBMEM : OpCodeHandlerModRM
	{
		// Token: 0x0600268E RID: 9870 RVA: 0x00084260 File Offset: 0x00082460
		public OpCodeHandler_VEX_VT_SIBMEM(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600268F RID: 9871 RVA: 0x00084270 File Offset: 0x00082470
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (((decoder.state.vvvv_invalidCheck | decoder.state.zs.extraRegisterBase) & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.TMM0;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMemSib(ref instruction);
		}

		// Token: 0x040038EE RID: 14574
		private readonly Code code;
	}
}
