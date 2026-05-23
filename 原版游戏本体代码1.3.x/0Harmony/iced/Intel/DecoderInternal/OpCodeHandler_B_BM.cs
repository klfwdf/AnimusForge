using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200078F RID: 1935
	internal sealed class OpCodeHandler_B_BM : OpCodeHandlerModRM
	{
		// Token: 0x0600260F RID: 9743 RVA: 0x00081284 File Offset: 0x0007F484
		public OpCodeHandler_B_BM(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002610 RID: 9744 RVA: 0x0008129C File Offset: 0x0007F49C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.reg > 3U || (decoder.state.zs.extraRegisterBase & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			if (decoder.is64bMode)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
			}
			instruction.Op0Register = (int)decoder.state.reg + Register.BND0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)decoder.state.rm + Register.BND0;
				if (decoder.state.rm > 3U || (decoder.state.zs.extraBaseRegisterBase & decoder.invalidCheckMask) != 0U)
				{
					decoder.SetInvalidInstruction();
					return;
				}
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem_MPX(ref instruction);
			}
		}

		// Token: 0x0400386E RID: 14446
		private readonly Code code32;

		// Token: 0x0400386F RID: 14447
		private readonly Code code64;
	}
}
