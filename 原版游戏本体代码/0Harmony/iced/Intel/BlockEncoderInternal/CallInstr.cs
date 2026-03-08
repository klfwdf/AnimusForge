using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007DE RID: 2014
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class CallInstr : Instr
	{
		// Token: 0x060026C5 RID: 9925 RVA: 0x000855E0 File Offset: 0x000837E0
		public CallInstr(BlockEncoder blockEncoder, Block block, in Instruction instruction)
			: base(block, instruction.IP)
		{
			this.bitness = (byte)blockEncoder.Bitness;
			this.instruction = instruction;
			Instruction instrCopy = instruction;
			instrCopy.NearBranch64 = 0UL;
			this.origInstructionSize = (byte)blockEncoder.GetInstructionSize(instrCopy, 0UL);
			if (!blockEncoder.FixBranches)
			{
				this.Size = (uint)this.origInstructionSize;
				this.useOrigInstruction = true;
				return;
			}
			if (blockEncoder.Bitness == 64)
			{
				this.Size = Math.Max((uint)this.origInstructionSize, 6U);
				return;
			}
			this.Size = (uint)this.origInstructionSize;
		}

		// Token: 0x060026C6 RID: 9926 RVA: 0x00085679 File Offset: 0x00083879
		public override void Initialize(BlockEncoder blockEncoder)
		{
			this.targetInstr = blockEncoder.GetTarget(this.instruction.NearBranchTarget);
		}

		// Token: 0x060026C7 RID: 9927 RVA: 0x00085692 File Offset: 0x00083892
		public override bool Optimize(ulong gained)
		{
			return this.TryOptimize(gained);
		}

		// Token: 0x060026C8 RID: 9928 RVA: 0x0008569C File Offset: 0x0008389C
		private bool TryOptimize(ulong gained)
		{
			if (this.Done || this.useOrigInstruction)
			{
				this.Done = true;
				return false;
			}
			bool useShort = this.bitness != 64 || this.targetInstr.IsInBlock(this.Block);
			if (!useShort)
			{
				long address = (long)this.targetInstr.GetAddress();
				ulong nextRip = this.IP + (ulong)this.origInstructionSize;
				long diff = address - (long)nextRip;
				diff = Instr.CorrectDiff(this.targetInstr.IsInBlock(this.Block), diff, gained);
				useShort = -2147483648L <= diff && diff <= 2147483647L;
			}
			if (useShort)
			{
				if (this.pointerData != null)
				{
					this.pointerData.IsValid = false;
				}
				this.Size = (uint)this.origInstructionSize;
				this.useOrigInstruction = true;
				this.Done = true;
				return true;
			}
			if (this.pointerData == null)
			{
				this.pointerData = this.Block.AllocPointerLocation();
			}
			return false;
		}

		// Token: 0x060026C9 RID: 9929 RVA: 0x00085780 File Offset: 0x00083980
		[return: Nullable(2)]
		public override string TryEncode(Encoder encoder, out ConstantOffsets constantOffsets, out bool isOriginalInstruction)
		{
			if (this.useOrigInstruction)
			{
				isOriginalInstruction = true;
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
			else
			{
				isOriginalInstruction = false;
				constantOffsets = default(ConstantOffsets);
				this.pointerData.Data = this.targetInstr.GetAddress();
				uint num;
				string errorMessage2 = base.EncodeBranchToPointerData(encoder, true, this.IP, this.pointerData, out num, this.Size);
				if (errorMessage2 != null)
				{
					return Instr.CreateErrorMessage(errorMessage2, this.instruction);
				}
				return null;
			}
		}

		// Token: 0x04003961 RID: 14689
		private readonly byte bitness;

		// Token: 0x04003962 RID: 14690
		private Instruction instruction;

		// Token: 0x04003963 RID: 14691
		private TargetInstr targetInstr;

		// Token: 0x04003964 RID: 14692
		private readonly byte origInstructionSize;

		// Token: 0x04003965 RID: 14693
		private BlockData pointerData;

		// Token: 0x04003966 RID: 14694
		private bool useOrigInstruction;
	}
}
