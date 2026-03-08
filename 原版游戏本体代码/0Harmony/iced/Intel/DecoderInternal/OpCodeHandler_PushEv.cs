using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200072E RID: 1838
	internal sealed class OpCodeHandler_PushEv : OpCodeHandlerModRM
	{
		// Token: 0x06002543 RID: 9539 RVA: 0x0007D2EE File Offset: 0x0007B4EE
		public OpCodeHandler_PushEv(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002544 RID: 9540 RVA: 0x0007D30C File Offset: 0x0007B50C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if (decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
				}
				else
				{
					instruction.InternalSetCodeNoCheck(this.code16);
				}
			}
			else if (decoder.state.operandSize == OpSize.Size32)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code16);
			}
			if (decoder.state.mod != 3U)
			{
				instruction.Op0Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
				return;
			}
			uint index = decoder.state.rm + decoder.state.zs.extraBaseRegisterBase;
			if (decoder.is64bMode)
			{
				if (decoder.state.operandSize != OpSize.Size16)
				{
					instruction.Op0Register = (int)index + Register.RAX;
					return;
				}
				instruction.Op0Register = (int)index + Register.AX;
				return;
			}
			else
			{
				if (decoder.state.operandSize == OpSize.Size32)
				{
					instruction.Op0Register = (int)index + Register.EAX;
					return;
				}
				instruction.Op0Register = (int)index + Register.AX;
				return;
			}
		}

		// Token: 0x040037C9 RID: 14281
		private readonly Code code16;

		// Token: 0x040037CA RID: 14282
		private readonly Code code32;

		// Token: 0x040037CB RID: 14283
		private readonly Code code64;
	}
}
