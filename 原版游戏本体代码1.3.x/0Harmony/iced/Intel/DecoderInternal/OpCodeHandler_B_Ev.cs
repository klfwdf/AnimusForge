using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000791 RID: 1937
	internal sealed class OpCodeHandler_B_Ev : OpCodeHandlerModRM
	{
		// Token: 0x06002613 RID: 9747 RVA: 0x0008145D File Offset: 0x0007F65D
		public OpCodeHandler_B_Ev(Code code32, Code code64, bool supportsRipRel)
		{
			this.code32 = code32;
			this.code64 = code64;
			this.ripRelMask = (supportsRipRel ? 0U : uint.MaxValue);
		}

		// Token: 0x06002614 RID: 9748 RVA: 0x00081480 File Offset: 0x0007F680
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.reg > 3U || (decoder.state.zs.extraRegisterBase & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			Register baseReg;
			if (decoder.is64bMode)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				baseReg = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				baseReg = Register.EAX;
			}
			instruction.Op0Register = (int)decoder.state.reg + Register.BND0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + baseReg;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem_MPX(ref instruction);
			if ((this.ripRelMask & decoder.invalidCheckMask) != 0U && instruction.MemoryBase == Register.RIP)
			{
				decoder.SetInvalidInstruction();
			}
		}

		// Token: 0x04003872 RID: 14450
		private readonly Code code32;

		// Token: 0x04003873 RID: 14451
		private readonly Code code64;

		// Token: 0x04003874 RID: 14452
		private readonly uint ripRelMask;
	}
}
