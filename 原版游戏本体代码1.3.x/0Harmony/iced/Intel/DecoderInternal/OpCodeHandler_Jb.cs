using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000728 RID: 1832
	internal sealed class OpCodeHandler_Jb : OpCodeHandler
	{
		// Token: 0x06002537 RID: 9527 RVA: 0x0007CCF6 File Offset: 0x0007AEF6
		public OpCodeHandler_Jb(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002538 RID: 9528 RVA: 0x0007CD14 File Offset: 0x0007AF14
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.BranchImm8;
			if (decoder.is64bMode)
			{
				if ((decoder.options & DecoderOptions.AMD) == DecoderOptions.None || decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
					instruction.Op0Kind = OpKind.NearBranch64;
					instruction.NearBranch64 = (ulong)((long)((sbyte)decoder.ReadByte()) + (long)decoder.GetCurrentInstructionPointer64());
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Kind = OpKind.NearBranch16;
				instruction.InternalNearBranch16 = (uint)((ushort)((uint)((sbyte)decoder.ReadByte()) + decoder.GetCurrentInstructionPointer32()));
				return;
			}
			else
			{
				if (decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code32);
					instruction.Op0Kind = OpKind.NearBranch32;
					instruction.NearBranch32 = (uint)((sbyte)decoder.ReadByte()) + decoder.GetCurrentInstructionPointer32();
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Kind = OpKind.NearBranch16;
				instruction.InternalNearBranch16 = (uint)((ushort)((uint)((sbyte)decoder.ReadByte()) + decoder.GetCurrentInstructionPointer32()));
				return;
			}
		}

		// Token: 0x040037B3 RID: 14259
		private readonly Code code16;

		// Token: 0x040037B4 RID: 14260
		private readonly Code code32;

		// Token: 0x040037B5 RID: 14261
		private readonly Code code64;
	}
}
