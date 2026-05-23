using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000784 RID: 1924
	internal sealed class OpCodeHandler_VWIb : OpCodeHandlerModRM
	{
		// Token: 0x060025F8 RID: 9720 RVA: 0x00080A93 File Offset: 0x0007EC93
		public OpCodeHandler_VWIb(Code code)
		{
			this.codeW0 = code;
			this.codeW1 = code;
		}

		// Token: 0x060025F9 RID: 9721 RVA: 0x00080AA9 File Offset: 0x0007ECA9
		public OpCodeHandler_VWIb(Code codeW0, Code codeW1)
		{
			this.codeW0 = codeW0;
			this.codeW1 = codeW1;
		}

		// Token: 0x060025FA RID: 9722 RVA: 0x00080AC0 File Offset: 0x0007ECC0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.codeW1);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.codeW0);
			}
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x0400385D RID: 14429
		private readonly Code codeW0;

		// Token: 0x0400385E RID: 14430
		private readonly Code codeW1;
	}
}
