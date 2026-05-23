using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007AD RID: 1965
	internal sealed class OpCodeHandler_VEX_Ed_V_Ib : OpCodeHandlerModRM
	{
		// Token: 0x0600264D RID: 9805 RVA: 0x00082960 File Offset: 0x00080B60
		public OpCodeHandler_VEX_Ed_V_Ib(Register baseReg, Code code32, Code code64)
		{
			this.baseReg = baseReg;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600264E RID: 9806 RVA: 0x00082980 File Offset: 0x00080B80
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			Register gpr;
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				gpr = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				gpr = Register.EAX;
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
			}
			else
			{
				instruction.Op0Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg;
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040038A9 RID: 14505
		private readonly Register baseReg;

		// Token: 0x040038AA RID: 14506
		private readonly Code code32;

		// Token: 0x040038AB RID: 14507
		private readonly Code code64;
	}
}
