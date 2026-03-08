using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200074A RID: 1866
	internal sealed class OpCodeHandler_Gv_Ev_32_64 : OpCodeHandlerModRM
	{
		// Token: 0x0600257E RID: 9598 RVA: 0x0007E6D5 File Offset: 0x0007C8D5
		public OpCodeHandler_Gv_Ev_32_64(Code code32, Code code64, bool allowReg, bool allowMem)
		{
			this.code32 = code32;
			this.code64 = code64;
			this.disallowMem = (allowMem ? 0U : uint.MaxValue);
			this.disallowReg = (allowReg ? 0U : uint.MaxValue);
		}

		// Token: 0x0600257F RID: 9599 RVA: 0x0007E708 File Offset: 0x0007C908
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
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
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + baseReg;
				if ((this.disallowReg & decoder.invalidCheckMask) != 0U)
				{
					decoder.SetInvalidInstruction();
					return;
				}
			}
			else
			{
				if ((this.disallowMem & decoder.invalidCheckMask) != 0U)
				{
					decoder.SetInvalidInstruction();
				}
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
		}

		// Token: 0x04003801 RID: 14337
		private readonly Code code32;

		// Token: 0x04003802 RID: 14338
		private readonly Code code64;

		// Token: 0x04003803 RID: 14339
		private readonly uint disallowReg;

		// Token: 0x04003804 RID: 14340
		private readonly uint disallowMem;
	}
}
