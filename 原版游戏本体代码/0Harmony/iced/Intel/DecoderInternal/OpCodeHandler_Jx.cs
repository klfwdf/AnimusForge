using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000729 RID: 1833
	internal sealed class OpCodeHandler_Jx : OpCodeHandler
	{
		// Token: 0x06002539 RID: 9529 RVA: 0x0007CE08 File Offset: 0x0007B008
		public OpCodeHandler_Jx(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600253A RID: 9530 RVA: 0x0007CE28 File Offset: 0x0007B028
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.Xbegin;
			if (decoder.is64bMode)
			{
				if (decoder.state.operandSize == OpSize.Size32)
				{
					instruction.InternalSetCodeNoCheck(this.code32);
					instruction.Op0Kind = OpKind.NearBranch64;
					instruction.NearBranch64 = (ulong)((long)decoder.ReadUInt32() + (long)decoder.GetCurrentInstructionPointer64());
					return;
				}
				if (decoder.state.operandSize == OpSize.Size64)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
					instruction.Op0Kind = OpKind.NearBranch64;
					instruction.NearBranch64 = (ulong)((long)decoder.ReadUInt32() + (long)decoder.GetCurrentInstructionPointer64());
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Kind = OpKind.NearBranch64;
				instruction.NearBranch64 = (ulong)((long)((short)decoder.ReadUInt16()) + (long)decoder.GetCurrentInstructionPointer64());
				return;
			}
			else
			{
				if (decoder.state.operandSize == OpSize.Size32)
				{
					instruction.InternalSetCodeNoCheck(this.code32);
					instruction.Op0Kind = OpKind.NearBranch32;
					instruction.NearBranch32 = decoder.ReadUInt32() + decoder.GetCurrentInstructionPointer32();
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Kind = OpKind.NearBranch32;
				instruction.NearBranch32 = (uint)((short)decoder.ReadUInt16()) + decoder.GetCurrentInstructionPointer32();
				return;
			}
		}

		// Token: 0x040037B6 RID: 14262
		private readonly Code code16;

		// Token: 0x040037B7 RID: 14263
		private readonly Code code32;

		// Token: 0x040037B8 RID: 14264
		private readonly Code code64;
	}
}
