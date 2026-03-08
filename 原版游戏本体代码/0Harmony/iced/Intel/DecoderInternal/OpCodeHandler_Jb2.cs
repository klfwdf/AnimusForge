using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200072B RID: 1835
	internal sealed class OpCodeHandler_Jb2 : OpCodeHandler
	{
		// Token: 0x0600253D RID: 9533 RVA: 0x0007D03F File Offset: 0x0007B23F
		public OpCodeHandler_Jb2(Code code16_16, Code code16_32, Code code16_64, Code code32_16, Code code32_32, Code code64_32, Code code64_64)
		{
			this.code16_16 = code16_16;
			this.code16_32 = code16_32;
			this.code16_64 = code16_64;
			this.code32_16 = code32_16;
			this.code32_32 = code32_32;
			this.code64_32 = code64_32;
			this.code64_64 = code64_64;
		}

		// Token: 0x0600253E RID: 9534 RVA: 0x0007D07C File Offset: 0x0007B27C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.BranchImm8;
			if (decoder.is64bMode)
			{
				if ((decoder.options & DecoderOptions.AMD) == DecoderOptions.None || decoder.state.operandSize != OpSize.Size16)
				{
					if (decoder.state.addressSize == OpSize.Size64)
					{
						instruction.InternalSetCodeNoCheck(this.code64_64);
					}
					else
					{
						instruction.InternalSetCodeNoCheck(this.code64_32);
					}
					instruction.Op0Kind = OpKind.NearBranch64;
					instruction.NearBranch64 = (ulong)((long)((sbyte)decoder.ReadByte()) + (long)decoder.GetCurrentInstructionPointer64());
					return;
				}
				if (decoder.state.addressSize == OpSize.Size64)
				{
					instruction.InternalSetCodeNoCheck(this.code16_64);
				}
				else
				{
					instruction.InternalSetCodeNoCheck(this.code16_32);
				}
				instruction.Op0Kind = OpKind.NearBranch16;
				instruction.InternalNearBranch16 = (uint)((ushort)((uint)((sbyte)decoder.ReadByte()) + decoder.GetCurrentInstructionPointer32()));
				return;
			}
			else
			{
				if (decoder.state.operandSize == OpSize.Size32)
				{
					if (decoder.state.addressSize == OpSize.Size32)
					{
						instruction.InternalSetCodeNoCheck(this.code32_32);
					}
					else
					{
						instruction.InternalSetCodeNoCheck(this.code32_16);
					}
					instruction.Op0Kind = OpKind.NearBranch32;
					instruction.NearBranch32 = (uint)((sbyte)decoder.ReadByte()) + decoder.GetCurrentInstructionPointer32();
					return;
				}
				if (decoder.state.addressSize == OpSize.Size32)
				{
					instruction.InternalSetCodeNoCheck(this.code16_32);
				}
				else
				{
					instruction.InternalSetCodeNoCheck(this.code16_16);
				}
				instruction.Op0Kind = OpKind.NearBranch16;
				instruction.InternalNearBranch16 = (uint)((ushort)((uint)((sbyte)decoder.ReadByte()) + decoder.GetCurrentInstructionPointer32()));
				return;
			}
		}

		// Token: 0x040037BC RID: 14268
		private readonly Code code16_16;

		// Token: 0x040037BD RID: 14269
		private readonly Code code16_32;

		// Token: 0x040037BE RID: 14270
		private readonly Code code16_64;

		// Token: 0x040037BF RID: 14271
		private readonly Code code32_16;

		// Token: 0x040037C0 RID: 14272
		private readonly Code code32_32;

		// Token: 0x040037C1 RID: 14273
		private readonly Code code64_32;

		// Token: 0x040037C2 RID: 14274
		private readonly Code code64_64;
	}
}
