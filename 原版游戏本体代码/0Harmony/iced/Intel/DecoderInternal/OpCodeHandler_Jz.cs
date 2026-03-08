using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200072A RID: 1834
	internal sealed class OpCodeHandler_Jz : OpCodeHandler
	{
		// Token: 0x0600253B RID: 9531 RVA: 0x0007CF4A File Offset: 0x0007B14A
		public OpCodeHandler_Jz(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600253C RID: 9532 RVA: 0x0007CF68 File Offset: 0x0007B168
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if ((decoder.options & DecoderOptions.AMD) == DecoderOptions.None || decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
					instruction.Op0Kind = OpKind.NearBranch64;
					instruction.NearBranch64 = (ulong)((long)decoder.ReadUInt32() + (long)decoder.GetCurrentInstructionPointer64());
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Kind = OpKind.NearBranch16;
				instruction.InternalNearBranch16 = (uint)((ushort)(decoder.ReadUInt16() + decoder.GetCurrentInstructionPointer32()));
				return;
			}
			else
			{
				if (decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code32);
					instruction.Op0Kind = OpKind.NearBranch32;
					instruction.NearBranch32 = decoder.ReadUInt32() + decoder.GetCurrentInstructionPointer32();
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Kind = OpKind.NearBranch16;
				instruction.InternalNearBranch16 = (uint)((ushort)(decoder.ReadUInt16() + decoder.GetCurrentInstructionPointer32()));
				return;
			}
		}

		// Token: 0x040037B9 RID: 14265
		private readonly Code code16;

		// Token: 0x040037BA RID: 14266
		private readonly Code code32;

		// Token: 0x040037BB RID: 14267
		private readonly Code code64;
	}
}
