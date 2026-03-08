using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007E5 RID: 2021
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class JmpInstr : Instr
	{
		// Token: 0x060026E1 RID: 9953 RVA: 0x00086530 File Offset: 0x00084730
		public JmpInstr(BlockEncoder blockEncoder, Block block, in Instruction instruction)
			: base(block, instruction.IP)
		{
			this.bitness = (byte)blockEncoder.Bitness;
			this.instruction = instruction;
			this.instrKind = JmpInstr.InstrKind.Uninitialized;
			Instruction instrCopy;
			if (!blockEncoder.FixBranches)
			{
				this.instrKind = JmpInstr.InstrKind.Unchanged;
				instrCopy = instruction;
				instrCopy.NearBranch64 = 0UL;
				this.Size = blockEncoder.GetInstructionSize(instrCopy, 0UL);
				return;
			}
			instrCopy = instruction;
			instrCopy.InternalSetCodeNoCheck(instruction.Code.ToShortBranch());
			instrCopy.NearBranch64 = 0UL;
			this.shortInstructionSize = (byte)blockEncoder.GetInstructionSize(instrCopy, 0UL);
			instrCopy = instruction;
			instrCopy.InternalSetCodeNoCheck(instruction.Code.ToNearBranch());
			instrCopy.NearBranch64 = 0UL;
			this.nearInstructionSize = (byte)blockEncoder.GetInstructionSize(instrCopy, 0UL);
			if (blockEncoder.Bitness == 64)
			{
				this.Size = Math.Max((uint)this.nearInstructionSize, 6U);
				return;
			}
			this.Size = (uint)this.nearInstructionSize;
		}

		// Token: 0x060026E2 RID: 9954 RVA: 0x00086629 File Offset: 0x00084829
		public override void Initialize(BlockEncoder blockEncoder)
		{
			this.targetInstr = blockEncoder.GetTarget(this.instruction.NearBranchTarget);
		}

		// Token: 0x060026E3 RID: 9955 RVA: 0x00086642 File Offset: 0x00084842
		public override bool Optimize(ulong gained)
		{
			return this.TryOptimize(gained);
		}

		// Token: 0x060026E4 RID: 9956 RVA: 0x0008664C File Offset: 0x0008484C
		private bool TryOptimize(ulong gained)
		{
			if (this.instrKind == JmpInstr.InstrKind.Unchanged || this.instrKind == JmpInstr.InstrKind.Short)
			{
				this.Done = true;
				return false;
			}
			long address = (long)this.targetInstr.GetAddress();
			ulong nextRip = this.IP + (ulong)this.shortInstructionSize;
			long diff = address - (long)nextRip;
			diff = Instr.ConvertDiffToBitnessDiff((int)this.bitness, Instr.CorrectDiff(this.targetInstr.IsInBlock(this.Block), diff, gained));
			if (-128L <= diff && diff <= 127L)
			{
				if (this.pointerData != null)
				{
					this.pointerData.IsValid = false;
				}
				this.instrKind = JmpInstr.InstrKind.Short;
				this.Size = (uint)this.shortInstructionSize;
				this.Done = true;
				return true;
			}
			bool useNear = this.bitness != 64 || this.targetInstr.IsInBlock(this.Block);
			if (!useNear)
			{
				long address2 = (long)this.targetInstr.GetAddress();
				nextRip = this.IP + (ulong)this.nearInstructionSize;
				diff = address2 - (long)nextRip;
				diff = Instr.ConvertDiffToBitnessDiff((int)this.bitness, Instr.CorrectDiff(this.targetInstr.IsInBlock(this.Block), diff, gained));
				useNear = -2147483648L <= diff && diff <= 2147483647L;
			}
			if (useNear)
			{
				if (this.pointerData != null)
				{
					this.pointerData.IsValid = false;
				}
				if (diff < -1920L || diff > 1905L)
				{
					this.Done = true;
				}
				this.instrKind = JmpInstr.InstrKind.Near;
				this.Size = (uint)this.nearInstructionSize;
				return true;
			}
			if (this.pointerData == null)
			{
				this.pointerData = this.Block.AllocPointerLocation();
			}
			this.instrKind = JmpInstr.InstrKind.Long;
			return false;
		}

		// Token: 0x060026E5 RID: 9957 RVA: 0x000867D4 File Offset: 0x000849D4
		[return: Nullable(2)]
		public override string TryEncode(Encoder encoder, out ConstantOffsets constantOffsets, out bool isOriginalInstruction)
		{
			switch (this.instrKind)
			{
			case JmpInstr.InstrKind.Unchanged:
			case JmpInstr.InstrKind.Short:
			case JmpInstr.InstrKind.Near:
			{
				isOriginalInstruction = true;
				if (this.instrKind != JmpInstr.InstrKind.Unchanged)
				{
					if (this.instrKind == JmpInstr.InstrKind.Short)
					{
						this.instruction.InternalSetCodeNoCheck(this.instruction.Code.ToShortBranch());
					}
					else
					{
						this.instruction.InternalSetCodeNoCheck(this.instruction.Code.ToNearBranch());
					}
				}
				this.instruction.NearBranch64 = this.targetInstr.GetAddress();
				uint num;
				string errorMessage;
				if (!encoder.TryEncode(this.instruction, this.IP, out num, out errorMessage))
				{
					constantOffsets = default(ConstantOffsets);
					return Instr.CreateErrorMessage(errorMessage, this.instruction);
				}
				constantOffsets = encoder.GetConstantOffsets();
				return null;
			}
			case JmpInstr.InstrKind.Long:
			{
				isOriginalInstruction = false;
				constantOffsets = default(ConstantOffsets);
				this.pointerData.Data = this.targetInstr.GetAddress();
				uint num;
				string errorMessage = base.EncodeBranchToPointerData(encoder, false, this.IP, this.pointerData, out num, this.Size);
				if (errorMessage != null)
				{
					return Instr.CreateErrorMessage(errorMessage, this.instruction);
				}
				return null;
			}
			}
			throw new InvalidOperationException();
		}

		// Token: 0x04003988 RID: 14728
		private readonly byte bitness;

		// Token: 0x04003989 RID: 14729
		private Instruction instruction;

		// Token: 0x0400398A RID: 14730
		private TargetInstr targetInstr;

		// Token: 0x0400398B RID: 14731
		private BlockData pointerData;

		// Token: 0x0400398C RID: 14732
		private JmpInstr.InstrKind instrKind;

		// Token: 0x0400398D RID: 14733
		private readonly byte shortInstructionSize;

		// Token: 0x0400398E RID: 14734
		private readonly byte nearInstructionSize;

		// Token: 0x020007E6 RID: 2022
		private enum InstrKind : byte
		{
			// Token: 0x04003990 RID: 14736
			Unchanged,
			// Token: 0x04003991 RID: 14737
			Short,
			// Token: 0x04003992 RID: 14738
			Near,
			// Token: 0x04003993 RID: 14739
			Long,
			// Token: 0x04003994 RID: 14740
			Uninitialized
		}
	}
}
