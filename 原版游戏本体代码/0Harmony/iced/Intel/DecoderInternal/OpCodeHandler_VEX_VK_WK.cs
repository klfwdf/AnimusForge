using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007BC RID: 1980
	internal sealed class OpCodeHandler_VEX_VK_WK : OpCodeHandlerModRM
	{
		// Token: 0x0600266E RID: 9838 RVA: 0x0008351B File Offset: 0x0008171B
		public OpCodeHandler_VEX_VK_WK(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600266F RID: 9839 RVA: 0x0008352C File Offset: 0x0008172C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (((decoder.state.vvvv_invalidCheck | decoder.state.zs.extraRegisterBase) & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.K0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)decoder.state.rm + Register.K0;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038CA RID: 14538
		private readonly Code code;
	}
}
