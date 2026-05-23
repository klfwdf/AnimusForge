using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007AB RID: 1963
	internal sealed class OpCodeHandler_VEX_VWIb : OpCodeHandlerModRM
	{
		// Token: 0x06002648 RID: 9800 RVA: 0x00082762 File Offset: 0x00080962
		public OpCodeHandler_VEX_VWIb(Register baseReg, Code code)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.codeW0 = code;
			this.codeW1 = code;
		}

		// Token: 0x06002649 RID: 9801 RVA: 0x00082786 File Offset: 0x00080986
		public OpCodeHandler_VEX_VWIb(Register baseReg, Code codeW0, Code codeW1)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
		}

		// Token: 0x0600264A RID: 9802 RVA: 0x000827AC File Offset: 0x000809AC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.codeW1);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.codeW0);
			}
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + this.baseReg1;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + this.baseReg2;
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040038A2 RID: 14498
		private readonly Register baseReg1;

		// Token: 0x040038A3 RID: 14499
		private readonly Register baseReg2;

		// Token: 0x040038A4 RID: 14500
		private readonly Code codeW0;

		// Token: 0x040038A5 RID: 14501
		private readonly Code codeW1;
	}
}
