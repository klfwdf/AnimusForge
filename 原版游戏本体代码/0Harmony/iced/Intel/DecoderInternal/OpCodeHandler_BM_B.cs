using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000790 RID: 1936
	internal sealed class OpCodeHandler_BM_B : OpCodeHandlerModRM
	{
		// Token: 0x06002611 RID: 9745 RVA: 0x00081371 File Offset: 0x0007F571
		public OpCodeHandler_BM_B(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002612 RID: 9746 RVA: 0x00081388 File Offset: 0x0007F588
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
			instruction.Op1Register = (int)decoder.state.reg + Register.BND0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)decoder.state.rm + Register.BND0;
				if (decoder.state.rm > 3U || (decoder.state.zs.extraBaseRegisterBase & decoder.invalidCheckMask) != 0U)
				{
					decoder.SetInvalidInstruction();
					return;
				}
			}
			else
			{
				instruction.Op0Kind = OpKind.Memory;
				decoder.ReadOpMem_MPX(ref instruction);
			}
		}

		// Token: 0x04003870 RID: 14448
		private readonly Code code32;

		// Token: 0x04003871 RID: 14449
		private readonly Code code64;
	}
}
