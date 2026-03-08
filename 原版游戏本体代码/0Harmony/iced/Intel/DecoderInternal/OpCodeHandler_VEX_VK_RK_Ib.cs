using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007BB RID: 1979
	internal sealed class OpCodeHandler_VEX_VK_RK_Ib : OpCodeHandlerModRM
	{
		// Token: 0x0600266C RID: 9836 RVA: 0x0008346F File Offset: 0x0008166F
		public OpCodeHandler_VEX_VK_RK_Ib(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600266D RID: 9837 RVA: 0x00083480 File Offset: 0x00081680
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
			}
			else
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040038C9 RID: 14537
		private readonly Code code;
	}
}
