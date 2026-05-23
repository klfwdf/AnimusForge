using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007E0 RID: 2016
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class Instr
	{
		// Token: 0x060026CC RID: 9932 RVA: 0x00085869 File Offset: 0x00083A69
		protected Instr(Block block, ulong origIp)
		{
			this.OrigIP = origIp;
			this.Block = block;
		}

		// Token: 0x060026CD RID: 9933
		public abstract void Initialize(BlockEncoder blockEncoder);

		// Token: 0x060026CE RID: 9934
		public abstract bool Optimize(ulong gained);

		// Token: 0x060026CF RID: 9935
		[return: Nullable(2)]
		public abstract string TryEncode(Encoder encoder, out ConstantOffsets constantOffsets, out bool isOriginalInstruction);

		// Token: 0x060026D0 RID: 9936 RVA: 0x00085880 File Offset: 0x00083A80
		protected static string CreateErrorMessage(string errorMessage, in Instruction instruction)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 3);
			defaultInterpolatedStringHandler.AppendFormatted(errorMessage);
			defaultInterpolatedStringHandler.AppendLiteral(" : 0x");
			defaultInterpolatedStringHandler.AppendFormatted<ulong>(instruction.IP, "X");
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(instruction.ToString());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060026D1 RID: 9937 RVA: 0x000858E4 File Offset: 0x00083AE4
		public static Instr Create(BlockEncoder blockEncoder, Block block, in Instruction instruction)
		{
			Code code = instruction.Code;
			if (code <= Code.Jmp_rel8_64)
			{
				if (code - Code.Jo_rel8_16 > 47)
				{
					if (code - Code.Xbegin_rel16 <= 1)
					{
						return new XbeginInstr(blockEncoder, block, ref instruction);
					}
					switch (code)
					{
					case Code.Loopne_rel8_16_CX:
					case Code.Loopne_rel8_32_CX:
					case Code.Loopne_rel8_16_ECX:
					case Code.Loopne_rel8_32_ECX:
					case Code.Loopne_rel8_64_ECX:
					case Code.Loopne_rel8_16_RCX:
					case Code.Loopne_rel8_64_RCX:
					case Code.Loope_rel8_16_CX:
					case Code.Loope_rel8_32_CX:
					case Code.Loope_rel8_16_ECX:
					case Code.Loope_rel8_32_ECX:
					case Code.Loope_rel8_64_ECX:
					case Code.Loope_rel8_16_RCX:
					case Code.Loope_rel8_64_RCX:
					case Code.Loop_rel8_16_CX:
					case Code.Loop_rel8_32_CX:
					case Code.Loop_rel8_16_ECX:
					case Code.Loop_rel8_32_ECX:
					case Code.Loop_rel8_64_ECX:
					case Code.Loop_rel8_16_RCX:
					case Code.Loop_rel8_64_RCX:
					case Code.Jcxz_rel8_16:
					case Code.Jcxz_rel8_32:
					case Code.Jecxz_rel8_16:
					case Code.Jecxz_rel8_32:
					case Code.Jecxz_rel8_64:
					case Code.Jrcxz_rel8_16:
					case Code.Jrcxz_rel8_64:
						return new SimpleBranchInstr(blockEncoder, block, ref instruction);
					case Code.In_AL_imm8:
					case Code.In_AX_imm8:
					case Code.In_EAX_imm8:
					case Code.Out_imm8_AL:
					case Code.Out_imm8_AX:
					case Code.Out_imm8_EAX:
					case Code.Jmp_ptr1616:
					case Code.Jmp_ptr1632:
						goto IL_13B;
					case Code.Call_rel16:
					case Code.Call_rel32_32:
					case Code.Call_rel32_64:
						return new CallInstr(blockEncoder, block, ref instruction);
					case Code.Jmp_rel16:
					case Code.Jmp_rel32_32:
					case Code.Jmp_rel32_64:
					case Code.Jmp_rel8_16:
					case Code.Jmp_rel8_32:
					case Code.Jmp_rel8_64:
						return new JmpInstr(blockEncoder, block, ref instruction);
					default:
						goto IL_13B;
					}
				}
			}
			else if (code - Code.Jo_rel16 > 47 && code - Code.VEX_KNC_Jkzd_kr_rel8_64 > 1 && code - Code.VEX_KNC_Jkzd_kr_rel32_64 > 1)
			{
				goto IL_13B;
			}
			return new JccInstr(blockEncoder, block, ref instruction);
			IL_13B:
			if (blockEncoder.Bitness == 64)
			{
				int ops = instruction.OpCount;
				int i = 0;
				while (i < ops)
				{
					if (instruction.GetOpKind(i) == OpKind.Memory)
					{
						if (instruction.IsIPRelativeMemoryOperand)
						{
							return new IpRelMemOpInstr(blockEncoder, block, ref instruction);
						}
						break;
					}
					else
					{
						i++;
					}
				}
			}
			return new SimpleInstr(blockEncoder, block, ref instruction);
		}

		// Token: 0x060026D2 RID: 9938 RVA: 0x00085A70 File Offset: 0x00083C70
		[return: Nullable(2)]
		protected string EncodeBranchToPointerData(Encoder encoder, bool isCall, ulong ip, BlockData pointerData, out uint size, uint minSize)
		{
			if (minSize > 2147483647U)
			{
				throw new ArgumentOutOfRangeException("minSize");
			}
			Instruction instr = default(Instruction);
			instr.Op0Kind = OpKind.Memory;
			instr.MemoryDisplSize = encoder.Bitness / 8;
			if (encoder.Bitness != 64)
			{
				throw new InvalidOperationException();
			}
			instr.InternalSetCodeNoCheck(isCall ? Code.Call_rm64 : Code.Jmp_rm64);
			instr.MemoryBase = Register.RIP;
			ulong nextRip = ip + 6UL;
			long diff = (long)(pointerData.Address - nextRip);
			if (-2147483648L > diff || diff > 2147483647L)
			{
				size = 0U;
				return "Block is too big";
			}
			instr.MemoryDisplacement64 = pointerData.Address;
			RelocKind relocKind = RelocKind.Offset64;
			string errorMessage;
			if (!encoder.TryEncode(instr, ip, out size, out errorMessage))
			{
				return errorMessage;
			}
			if (this.Block.CanAddRelocInfos && relocKind != RelocKind.Offset64)
			{
				ConstantOffsets constantOffsets = encoder.GetConstantOffsets();
				if (!constantOffsets.HasDisplacement)
				{
					return "Internal error: no displ";
				}
				this.Block.AddRelocInfo(new RelocInfo(relocKind, this.IP + (ulong)constantOffsets.DisplacementOffset));
			}
			while (size < minSize)
			{
				size += 1U;
				this.Block.CodeWriter.WriteByte(144);
			}
			return null;
		}

		// Token: 0x060026D3 RID: 9939 RVA: 0x00085B9F File Offset: 0x00083D9F
		protected static long CorrectDiff(bool inBlock, long diff, ulong gained)
		{
			if (inBlock && diff >= 0L)
			{
				return diff - (long)gained;
			}
			return diff;
		}

		// Token: 0x060026D4 RID: 9940 RVA: 0x00085BB0 File Offset: 0x00083DB0
		protected static long ConvertDiffToBitnessDiff(int bitness, long diff)
		{
			long result;
			if (bitness != 16)
			{
				if (bitness != 32)
				{
					result = diff;
				}
				else
				{
					result = (long)((int)diff);
				}
			}
			else
			{
				result = (long)((short)diff);
			}
			return result;
		}

		// Token: 0x04003969 RID: 14697
		public readonly Block Block;

		// Token: 0x0400396A RID: 14698
		public uint Size;

		// Token: 0x0400396B RID: 14699
		public ulong IP;

		// Token: 0x0400396C RID: 14700
		public readonly ulong OrigIP;

		// Token: 0x0400396D RID: 14701
		public bool Done;

		// Token: 0x0400396E RID: 14702
		protected const uint CallOrJmpPointerDataInstructionSize64 = 6U;
	}
}
