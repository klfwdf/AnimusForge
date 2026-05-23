using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007A9 RID: 1961
	internal sealed class OpCodeHandler_VEX_RdRq : OpCodeHandlerModRM
	{
		// Token: 0x06002644 RID: 9796 RVA: 0x000825A8 File Offset: 0x000807A8
		public OpCodeHandler_VEX_RdRq(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002645 RID: 9797 RVA: 0x000825C0 File Offset: 0x000807C0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.EAX;
			}
			if (decoder.state.mod != 3U)
			{
				decoder.SetInvalidInstruction();
			}
		}

		// Token: 0x0400389E RID: 14494
		private readonly Code code32;

		// Token: 0x0400389F RID: 14495
		private readonly Code code64;
	}
}
