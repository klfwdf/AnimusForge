using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007EB RID: 2027
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class XbeginInstr : Instr
	{
		// Token: 0x060026F4 RID: 9972 RVA: 0x000871D8 File Offset: 0x000853D8
		public XbeginInstr(BlockEncoder blockEncoder, Block block, in Instruction instruction)
			: base(block, instruction.IP)
		{
			this.instruction = instruction;
			this.instrKind = XbeginInstr.InstrKind.Uninitialized;
			Instruction instrCopy;
			if (!blockEncoder.FixBranches)
			{
				this.instrKind = XbeginInstr.InstrKind.Unchanged;
				instrCopy = instruction;
				instrCopy.NearBranch64 = 0UL;
				this.Size = blockEncoder.GetInstructionSize(instrCopy, 0UL);
				return;
			}
			instrCopy = instruction;
			instrCopy.InternalSetCodeNoCheck(Code.Xbegin_rel16);
			instrCopy.NearBranch64 = 0UL;
			this.shortInstructionSize = (byte)blockEncoder.GetInstructionSize(instrCopy, 0UL);
			instrCopy = instruction;
			instrCopy.InternalSetCodeNoCheck(Code.Xbegin_rel32);
			instrCopy.NearBranch64 = 0UL;
			this.nearInstructionSize = (byte)blockEncoder.GetInstructionSize(instrCopy, 0UL);
			this.Size = (uint)this.nearInstructionSize;
		}

		// Token: 0x060026F5 RID: 9973 RVA: 0x0008729B File Offset: 0x0008549B
		public override void Initialize(BlockEncoder blockEncoder)
		{
			this.targetInstr = blockEncoder.GetTarget(this.instruction.NearBranchTarget);
		}

		// Token: 0x060026F6 RID: 9974 RVA: 0x000872B4 File Offset: 0x000854B4
		public override bool Optimize(ulong gained)
		{
			return this.TryOptimize(gained);
		}

		// Token: 0x060026F7 RID: 9975 RVA: 0x000872C0 File Offset: 0x000854C0
		private bool TryOptimize(ulong gained)
		{
			if (this.instrKind == XbeginInstr.InstrKind.Unchanged || this.instrKind == XbeginInstr.InstrKind.Rel16)
			{
				this.Done = true;
				return false;
			}
			long address = (long)this.targetInstr.GetAddress();
			ulong nextRip = this.IP + (ulong)this.shortInstructionSize;
			long diff = address - (long)nextRip;
			diff = Instr.CorrectDiff(this.targetInstr.IsInBlock(this.Block), diff, gained);
			if (-32768L <= diff && diff <= 32767L)
			{
				this.instrKind = XbeginInstr.InstrKind.Rel16;
				this.Size = (uint)this.shortInstructionSize;
				return true;
			}
			this.instrKind = XbeginInstr.InstrKind.Rel32;
			this.Size = (uint)this.nearInstructionSize;
			return false;
		}

		// Token: 0x060026F8 RID: 9976 RVA: 0x00087358 File Offset: 0x00085558
		[return: Nullable(2)]
		public override string TryEncode(Encoder encoder, out ConstantOffsets constantOffsets, out bool isOriginalInstruction)
		{
			XbeginInstr.InstrKind instrKind = this.instrKind;
			if (instrKind > XbeginInstr.InstrKind.Rel32)
			{
				if (instrKind != XbeginInstr.InstrKind.Uninitialized)
				{
				}
				throw new InvalidOperationException();
			}
			isOriginalInstruction = true;
			if (this.instrKind != XbeginInstr.InstrKind.Unchanged)
			{
				if (this.instrKind == XbeginInstr.InstrKind.Rel16)
				{
					this.instruction.InternalSetCodeNoCheck(Code.Xbegin_rel16);
				}
				else
				{
					this.instruction.InternalSetCodeNoCheck(Code.Xbegin_rel32);
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

		// Token: 0x040039A8 RID: 14760
		private Instruction instruction;

		// Token: 0x040039A9 RID: 14761
		private TargetInstr targetInstr;

		// Token: 0x040039AA RID: 14762
		private XbeginInstr.InstrKind instrKind;

		// Token: 0x040039AB RID: 14763
		private readonly byte shortInstructionSize;

		// Token: 0x040039AC RID: 14764
		private readonly byte nearInstructionSize;

		// Token: 0x020007EC RID: 2028
		private enum InstrKind : byte
		{
			// Token: 0x040039AE RID: 14766
			Unchanged,
			// Token: 0x040039AF RID: 14767
			Rel16,
			// Token: 0x040039B0 RID: 14768
			Rel32,
			// Token: 0x040039B1 RID: 14769
			Uninitialized
		}
	}
}
