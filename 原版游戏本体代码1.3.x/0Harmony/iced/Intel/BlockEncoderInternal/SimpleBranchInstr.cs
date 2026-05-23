using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007E7 RID: 2023
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class SimpleBranchInstr : Instr
	{
		// Token: 0x060026E6 RID: 9958 RVA: 0x000868F8 File Offset: 0x00084AF8
		public SimpleBranchInstr(BlockEncoder blockEncoder, Block block, in Instruction instruction)
			: base(block, instruction.IP)
		{
			this.bitness = (byte)blockEncoder.Bitness;
			this.instruction = instruction;
			this.instrKind = SimpleBranchInstr.InstrKind.Uninitialized;
			Instruction instrCopy;
			if (!blockEncoder.FixBranches)
			{
				this.instrKind = SimpleBranchInstr.InstrKind.Unchanged;
				instrCopy = instruction;
				instrCopy.NearBranch64 = 0UL;
				this.Size = blockEncoder.GetInstructionSize(instrCopy, 0UL);
				return;
			}
			instrCopy = instruction;
			instrCopy.NearBranch64 = 0UL;
			this.shortInstructionSize = (byte)blockEncoder.GetInstructionSize(instrCopy, 0UL);
			this.nativeCode = SimpleBranchInstr.ToNativeBranchCode(instruction.Code, blockEncoder.Bitness);
			if (this.nativeCode == instruction.Code)
			{
				this.nativeInstructionSize = this.shortInstructionSize;
			}
			else
			{
				instrCopy = instruction;
				instrCopy.InternalSetCodeNoCheck(this.nativeCode);
				instrCopy.NearBranch64 = 0UL;
				this.nativeInstructionSize = (byte)blockEncoder.GetInstructionSize(instrCopy, 0UL);
			}
			int num = blockEncoder.Bitness;
			int num2;
			if (num != 16)
			{
				if (num != 32 && num != 64)
				{
					throw new InvalidOperationException();
				}
				num2 = (int)(this.nativeInstructionSize + 2 + 5);
			}
			else
			{
				num2 = (int)(this.nativeInstructionSize + 2 + 3);
			}
			this.nearInstructionSize = (byte)num2;
			if (blockEncoder.Bitness == 64)
			{
				this.longInstructionSize = (byte)((long)(this.nativeInstructionSize + 2) + 6L);
				this.Size = (uint)Math.Max(Math.Max(this.shortInstructionSize, this.nearInstructionSize), this.longInstructionSize);
				return;
			}
			this.Size = (uint)Math.Max(this.shortInstructionSize, this.nearInstructionSize);
		}

		// Token: 0x060026E7 RID: 9959 RVA: 0x00086A7C File Offset: 0x00084C7C
		private static Code ToNativeBranchCode(Code code, int bitness)
		{
			Code c16;
			Code c17;
			Code c18;
			switch (code)
			{
			case Code.Loopne_rel8_16_CX:
			case Code.Loopne_rel8_32_CX:
				c16 = Code.Loopne_rel8_16_CX;
				c17 = Code.Loopne_rel8_32_CX;
				c18 = Code.INVALID;
				break;
			case Code.Loopne_rel8_16_ECX:
			case Code.Loopne_rel8_32_ECX:
			case Code.Loopne_rel8_64_ECX:
				c16 = Code.Loopne_rel8_16_ECX;
				c17 = Code.Loopne_rel8_32_ECX;
				c18 = Code.Loopne_rel8_64_ECX;
				break;
			case Code.Loopne_rel8_16_RCX:
			case Code.Loopne_rel8_64_RCX:
				c16 = Code.Loopne_rel8_16_RCX;
				c17 = Code.INVALID;
				c18 = Code.Loopne_rel8_64_RCX;
				break;
			case Code.Loope_rel8_16_CX:
			case Code.Loope_rel8_32_CX:
				c16 = Code.Loope_rel8_16_CX;
				c17 = Code.Loope_rel8_32_CX;
				c18 = Code.INVALID;
				break;
			case Code.Loope_rel8_16_ECX:
			case Code.Loope_rel8_32_ECX:
			case Code.Loope_rel8_64_ECX:
				c16 = Code.Loope_rel8_16_ECX;
				c17 = Code.Loope_rel8_32_ECX;
				c18 = Code.Loope_rel8_64_ECX;
				break;
			case Code.Loope_rel8_16_RCX:
			case Code.Loope_rel8_64_RCX:
				c16 = Code.Loope_rel8_16_RCX;
				c17 = Code.INVALID;
				c18 = Code.Loope_rel8_64_RCX;
				break;
			case Code.Loop_rel8_16_CX:
			case Code.Loop_rel8_32_CX:
				c16 = Code.Loop_rel8_16_CX;
				c17 = Code.Loop_rel8_32_CX;
				c18 = Code.INVALID;
				break;
			case Code.Loop_rel8_16_ECX:
			case Code.Loop_rel8_32_ECX:
			case Code.Loop_rel8_64_ECX:
				c16 = Code.Loop_rel8_16_ECX;
				c17 = Code.Loop_rel8_32_ECX;
				c18 = Code.Loop_rel8_64_ECX;
				break;
			case Code.Loop_rel8_16_RCX:
			case Code.Loop_rel8_64_RCX:
				c16 = Code.Loop_rel8_16_RCX;
				c17 = Code.INVALID;
				c18 = Code.Loop_rel8_64_RCX;
				break;
			case Code.Jcxz_rel8_16:
			case Code.Jcxz_rel8_32:
				c16 = Code.Jcxz_rel8_16;
				c17 = Code.Jcxz_rel8_32;
				c18 = Code.INVALID;
				break;
			case Code.Jecxz_rel8_16:
			case Code.Jecxz_rel8_32:
			case Code.Jecxz_rel8_64:
				c16 = Code.Jecxz_rel8_16;
				c17 = Code.Jecxz_rel8_32;
				c18 = Code.Jecxz_rel8_64;
				break;
			case Code.Jrcxz_rel8_16:
			case Code.Jrcxz_rel8_64:
				c16 = Code.Jrcxz_rel8_16;
				c17 = Code.INVALID;
				c18 = Code.Jrcxz_rel8_64;
				break;
			default:
				throw new ArgumentOutOfRangeException("code");
			}
			Code result;
			if (bitness != 16)
			{
				if (bitness != 32)
				{
					if (bitness != 64)
					{
						throw new ArgumentOutOfRangeException("bitness");
					}
					result = c18;
				}
				else
				{
					result = c17;
				}
			}
			else
			{
				result = c16;
			}
			return result;
		}

		// Token: 0x060026E8 RID: 9960 RVA: 0x00086C1D File Offset: 0x00084E1D
		public override void Initialize(BlockEncoder blockEncoder)
		{
			this.targetInstr = blockEncoder.GetTarget(this.instruction.NearBranchTarget);
		}

		// Token: 0x060026E9 RID: 9961 RVA: 0x00086C36 File Offset: 0x00084E36
		public override bool Optimize(ulong gained)
		{
			return this.TryOptimize(gained);
		}

		// Token: 0x060026EA RID: 9962 RVA: 0x00086C40 File Offset: 0x00084E40
		private bool TryOptimize(ulong gained)
		{
			if (this.instrKind == SimpleBranchInstr.InstrKind.Unchanged || this.instrKind == SimpleBranchInstr.InstrKind.Short)
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
				this.instrKind = SimpleBranchInstr.InstrKind.Short;
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
				this.instrKind = SimpleBranchInstr.InstrKind.Near;
				this.Size = (uint)this.nearInstructionSize;
				return true;
			}
			if (this.pointerData == null)
			{
				this.pointerData = this.Block.AllocPointerLocation();
			}
			this.instrKind = SimpleBranchInstr.InstrKind.Long;
			return false;
		}

		// Token: 0x060026EB RID: 9963 RVA: 0x00086DC8 File Offset: 0x00084FC8
		[return: Nullable(2)]
		public override string TryEncode(Encoder encoder, out ConstantOffsets constantOffsets, out bool isOriginalInstruction)
		{
			switch (this.instrKind)
			{
			case SimpleBranchInstr.InstrKind.Unchanged:
			case SimpleBranchInstr.InstrKind.Short:
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
			case SimpleBranchInstr.InstrKind.Near:
			{
				isOriginalInstruction = false;
				constantOffsets = default(ConstantOffsets);
				Instruction instr = this.instruction;
				instr.InternalSetCodeNoCheck(this.nativeCode);
				instr.NearBranch64 = this.IP + (ulong)this.nativeInstructionSize + 2UL;
				string errorMessage;
				uint size;
				if (!encoder.TryEncode(instr, this.IP, out size, out errorMessage))
				{
					return Instr.CreateErrorMessage(errorMessage, this.instruction);
				}
				instr = default(Instruction);
				instr.NearBranch64 = this.IP + (ulong)this.nearInstructionSize;
				int num2 = encoder.Bitness;
				Code codeNear;
				if (num2 != 16)
				{
					if (num2 != 32)
					{
						if (num2 != 64)
						{
							throw new InvalidOperationException();
						}
						instr.InternalSetCodeNoCheck(Code.Jmp_rel8_64);
						codeNear = Code.Jmp_rel32_64;
						instr.Op0Kind = OpKind.NearBranch64;
					}
					else
					{
						instr.InternalSetCodeNoCheck(Code.Jmp_rel8_32);
						codeNear = Code.Jmp_rel32_32;
						instr.Op0Kind = OpKind.NearBranch32;
					}
				}
				else
				{
					instr.InternalSetCodeNoCheck(Code.Jmp_rel8_16);
					codeNear = Code.Jmp_rel16;
					instr.Op0Kind = OpKind.NearBranch16;
				}
				uint instrLen;
				if (!encoder.TryEncode(instr, this.IP + (ulong)size, out instrLen, out errorMessage))
				{
					return Instr.CreateErrorMessage(errorMessage, this.instruction);
				}
				size += instrLen;
				instr.InternalSetCodeNoCheck(codeNear);
				instr.NearBranch64 = this.targetInstr.GetAddress();
				encoder.TryEncode(instr, this.IP + (ulong)size, out instrLen, out errorMessage);
				if (errorMessage != null)
				{
					return Instr.CreateErrorMessage(errorMessage, this.instruction);
				}
				return null;
			}
			case SimpleBranchInstr.InstrKind.Long:
			{
				isOriginalInstruction = false;
				constantOffsets = default(ConstantOffsets);
				this.pointerData.Data = this.targetInstr.GetAddress();
				Instruction instr = this.instruction;
				instr.InternalSetCodeNoCheck(this.nativeCode);
				instr.NearBranch64 = this.IP + (ulong)this.nativeInstructionSize + 2UL;
				string errorMessage;
				uint instrLen;
				if (!encoder.TryEncode(instr, this.IP, out instrLen, out errorMessage))
				{
					return Instr.CreateErrorMessage(errorMessage, this.instruction);
				}
				uint size = instrLen;
				instr = default(Instruction);
				instr.NearBranch64 = this.IP + (ulong)this.longInstructionSize;
				int num2 = encoder.Bitness;
				if (num2 != 16)
				{
					if (num2 != 32)
					{
						if (num2 != 64)
						{
							throw new InvalidOperationException();
						}
						instr.InternalSetCodeNoCheck(Code.Jmp_rel8_64);
						instr.Op0Kind = OpKind.NearBranch64;
					}
					else
					{
						instr.InternalSetCodeNoCheck(Code.Jmp_rel8_32);
						instr.Op0Kind = OpKind.NearBranch32;
					}
				}
				else
				{
					instr.InternalSetCodeNoCheck(Code.Jmp_rel8_16);
					instr.Op0Kind = OpKind.NearBranch16;
				}
				if (!encoder.TryEncode(instr, this.IP + (ulong)size, out instrLen, out errorMessage))
				{
					return Instr.CreateErrorMessage(errorMessage, this.instruction);
				}
				size += instrLen;
				uint num;
				errorMessage = base.EncodeBranchToPointerData(encoder, false, this.IP + (ulong)size, this.pointerData, out num, this.Size - size);
				if (errorMessage != null)
				{
					return Instr.CreateErrorMessage(errorMessage, this.instruction);
				}
				return null;
			}
			}
			throw new InvalidOperationException();
		}

		// Token: 0x04003995 RID: 14741
		private readonly byte bitness;

		// Token: 0x04003996 RID: 14742
		private Instruction instruction;

		// Token: 0x04003997 RID: 14743
		private TargetInstr targetInstr;

		// Token: 0x04003998 RID: 14744
		private BlockData pointerData;

		// Token: 0x04003999 RID: 14745
		private SimpleBranchInstr.InstrKind instrKind;

		// Token: 0x0400399A RID: 14746
		private readonly byte shortInstructionSize;

		// Token: 0x0400399B RID: 14747
		private readonly byte nearInstructionSize;

		// Token: 0x0400399C RID: 14748
		private readonly byte longInstructionSize;

		// Token: 0x0400399D RID: 14749
		private readonly byte nativeInstructionSize;

		// Token: 0x0400399E RID: 14750
		private readonly Code nativeCode;

		// Token: 0x020007E8 RID: 2024
		private enum InstrKind : byte
		{
			// Token: 0x040039A0 RID: 14752
			Unchanged,
			// Token: 0x040039A1 RID: 14753
			Short,
			// Token: 0x040039A2 RID: 14754
			Near,
			// Token: 0x040039A3 RID: 14755
			Long,
			// Token: 0x040039A4 RID: 14756
			Uninitialized
		}
	}
}
