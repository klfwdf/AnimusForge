using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000733 RID: 1843
	internal sealed class OpCodeHandler_Gv_Mp : OpCodeHandlerModRM
	{
		// Token: 0x0600254E RID: 9550 RVA: 0x0007D74E File Offset: 0x0007B94E
		public OpCodeHandler_Gv_Mp(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600254F RID: 9551 RVA: 0x0007D76C File Offset: 0x0007B96C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.operandSize == OpSize.Size64 && (decoder.options & DecoderOptions.AMD) == DecoderOptions.None)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.RAX;
			}
			else if (decoder.state.operandSize == OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.EAX;
			}
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040037D2 RID: 14290
		private readonly Code code16;

		// Token: 0x040037D3 RID: 14291
		private readonly Code code32;

		// Token: 0x040037D4 RID: 14292
		private readonly Code code64;
	}
}
