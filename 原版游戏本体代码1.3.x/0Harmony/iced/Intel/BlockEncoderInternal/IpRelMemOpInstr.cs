using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007E1 RID: 2017
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class IpRelMemOpInstr : Instr
	{
		// Token: 0x060026D5 RID: 9941 RVA: 0x00085BD8 File Offset: 0x00083DD8
		public IpRelMemOpInstr(BlockEncoder blockEncoder, Block block, in Instruction instruction)
			: base(block, instruction.IP)
		{
			this.instruction = instruction;
			this.instrKind = IpRelMemOpInstr.InstrKind.Uninitialized;
			Instruction instrCopy = instruction;
			instrCopy.MemoryBase = Register.RIP;
			instrCopy.MemoryDisplacement64 = 0UL;
			this.ripInstructionSize = (byte)blockEncoder.GetInstructionSize(instrCopy, instrCopy.IPRelativeMemoryAddress);
			instrCopy.MemoryBase = Register.EIP;
			this.eipInstructionSize = (byte)blockEncoder.GetInstructionSize(instrCopy, instrCopy.IPRelativeMemoryAddress);
			this.Size = (uint)this.eipInstructionSize;
		}

		// Token: 0x060026D6 RID: 9942 RVA: 0x00085C5F File Offset: 0x00083E5F
		public override void Initialize(BlockEncoder blockEncoder)
		{
			this.targetInstr = blockEncoder.GetTarget(this.instruction.IPRelativeMemoryAddress);
		}

		// Token: 0x060026D7 RID: 9943 RVA: 0x00085C78 File Offset: 0x00083E78
		public override bool Optimize(ulong gained)
		{
			return this.TryOptimize(gained);
		}

		// Token: 0x060026D8 RID: 9944 RVA: 0x00085C84 File Offset: 0x00083E84
		private bool TryOptimize(ulong gained)
		{
			if (this.instrKind == IpRelMemOpInstr.InstrKind.Unchanged || this.instrKind == IpRelMemOpInstr.InstrKind.Rip || this.instrKind == IpRelMemOpInstr.InstrKind.Eip)
			{
				this.Done = true;
				return false;
			}
			bool useRip = this.targetInstr.IsInBlock(this.Block);
			ulong targetAddress = this.targetInstr.GetAddress();
			if (!useRip)
			{
				ulong nextRip = this.IP + (ulong)this.ripInstructionSize;
				long diff = (long)(targetAddress - nextRip);
				diff = Instr.CorrectDiff(this.targetInstr.IsInBlock(this.Block), diff, gained);
				useRip = -2147483648L <= diff && diff <= 2147483647L;
			}
			if (useRip)
			{
				this.Size = (uint)this.ripInstructionSize;
				this.instrKind = IpRelMemOpInstr.InstrKind.Rip;
				this.Done = true;
				return true;
			}
			if (targetAddress <= (ulong)(-1))
			{
				this.Size = (uint)this.eipInstructionSize;
				this.instrKind = IpRelMemOpInstr.InstrKind.Eip;
				this.Done = true;
				return true;
			}
			this.instrKind = IpRelMemOpInstr.InstrKind.Long;
			return false;
		}

		// Token: 0x060026D9 RID: 9945 RVA: 0x00085D64 File Offset: 0x00083F64
		[return: Nullable(2)]
		public override string TryEncode(Encoder encoder, out ConstantOffsets constantOffsets, out bool isOriginalInstruction)
		{
			switch (this.instrKind)
			{
			case IpRelMemOpInstr.InstrKind.Unchanged:
			case IpRelMemOpInstr.InstrKind.Rip:
			case IpRelMemOpInstr.InstrKind.Eip:
			{
				isOriginalInstruction = true;
				if (this.instrKind == IpRelMemOpInstr.InstrKind.Rip)
				{
					this.instruction.MemoryBase = Register.RIP;
				}
				else if (this.instrKind == IpRelMemOpInstr.InstrKind.Eip)
				{
					this.instruction.MemoryBase = Register.EIP;
				}
				ulong targetAddress = this.targetInstr.GetAddress();
				this.instruction.MemoryDisplacement64 = targetAddress;
				uint num;
				string errorMessage;
				encoder.TryEncode(this.instruction, this.IP, out num, out errorMessage);
				if (this.instruction.IPRelativeMemoryAddress != ((this.instruction.MemoryBase == Register.EIP) ? ((ulong)((uint)targetAddress)) : targetAddress))
				{
					errorMessage = "Invalid IP relative address";
				}
				if (errorMessage != null)
				{
					constantOffsets = default(ConstantOffsets);
					return Instr.CreateErrorMessage(errorMessage, this.instruction);
				}
				constantOffsets = encoder.GetConstantOffsets();
				return null;
			}
			case IpRelMemOpInstr.InstrKind.Long:
				isOriginalInstruction = false;
				constantOffsets = default(ConstantOffsets);
				return "IP relative memory operand is too far away and isn't currently supported. Try to allocate memory close to the original instruction (+/-2GB).";
			}
			throw new InvalidOperationException();
		}

		// Token: 0x0400396F RID: 14703
		private Instruction instruction;

		// Token: 0x04003970 RID: 14704
		private IpRelMemOpInstr.InstrKind instrKind;

		// Token: 0x04003971 RID: 14705
		private readonly byte eipInstructionSize;

		// Token: 0x04003972 RID: 14706
		private readonly byte ripInstructionSize;

		// Token: 0x04003973 RID: 14707
		private TargetInstr targetInstr;

		// Token: 0x020007E2 RID: 2018
		private enum InstrKind : byte
		{
			// Token: 0x04003975 RID: 14709
			Unchanged,
			// Token: 0x04003976 RID: 14710
			Rip,
			// Token: 0x04003977 RID: 14711
			Eip,
			// Token: 0x04003978 RID: 14712
			Long,
			// Token: 0x04003979 RID: 14713
			Uninitialized
		}
	}
}
