using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200071B RID: 1819
	internal sealed class OpCodeHandler_Evj : OpCodeHandlerModRM
	{
		// Token: 0x0600251D RID: 9501 RVA: 0x0007C22B File Offset: 0x0007A42B
		public OpCodeHandler_Evj(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600251E RID: 9502 RVA: 0x0007C248 File Offset: 0x0007A448
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if ((decoder.options & DecoderOptions.AMD) == DecoderOptions.None || decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
				}
				else
				{
					instruction.InternalSetCodeNoCheck(this.code16);
				}
				if (decoder.state.mod < 3U)
				{
					instruction.Op0Kind = OpKind.Memory;
					decoder.ReadOpMem(ref instruction);
					return;
				}
				if ((decoder.options & DecoderOptions.AMD) == DecoderOptions.None || decoder.state.operandSize != OpSize.Size16)
				{
					instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.RAX;
					return;
				}
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
				return;
			}
			else
			{
				if (decoder.state.operandSize == OpSize.Size32)
				{
					instruction.InternalSetCodeNoCheck(this.code32);
				}
				else
				{
					instruction.InternalSetCodeNoCheck(this.code16);
				}
				if (decoder.state.mod < 3U)
				{
					instruction.Op0Kind = OpKind.Memory;
					decoder.ReadOpMem(ref instruction);
					return;
				}
				if (decoder.state.operandSize == OpSize.Size32)
				{
					instruction.Op0Register = (int)decoder.state.rm + Register.EAX;
					return;
				}
				instruction.Op0Register = (int)decoder.state.rm + Register.AX;
				return;
			}
		}

		// Token: 0x0400379C RID: 14236
		private readonly Code code16;

		// Token: 0x0400379D RID: 14237
		private readonly Code code32;

		// Token: 0x0400379E RID: 14238
		private readonly Code code64;
	}
}
