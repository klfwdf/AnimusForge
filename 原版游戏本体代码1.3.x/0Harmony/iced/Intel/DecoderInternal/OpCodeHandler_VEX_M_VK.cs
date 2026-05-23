using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007BD RID: 1981
	internal sealed class OpCodeHandler_VEX_M_VK : OpCodeHandlerModRM
	{
		// Token: 0x06002670 RID: 9840 RVA: 0x000835BD File Offset: 0x000817BD
		public OpCodeHandler_VEX_M_VK(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002671 RID: 9841 RVA: 0x000835CC File Offset: 0x000817CC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (((decoder.state.vvvv_invalidCheck | decoder.state.zs.extraRegisterBase) & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)decoder.state.reg + Register.K0;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038CB RID: 14539
		private readonly Code code;
	}
}
