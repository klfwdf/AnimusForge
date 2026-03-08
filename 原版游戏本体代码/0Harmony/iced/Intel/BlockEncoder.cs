using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Iced.Intel.BlockEncoderInternal;

namespace Iced.Intel
{
	// Token: 0x0200062E RID: 1582
	[NullableContext(2)]
	[Nullable(0)]
	internal sealed class BlockEncoder
	{
		// Token: 0x1700075F RID: 1887
		// (get) Token: 0x060020F2 RID: 8434 RVA: 0x00068553 File Offset: 0x00066753
		internal int Bitness
		{
			get
			{
				return this.bitness;
			}
		}

		// Token: 0x17000760 RID: 1888
		// (get) Token: 0x060020F3 RID: 8435 RVA: 0x0006855B File Offset: 0x0006675B
		internal bool FixBranches
		{
			get
			{
				return (this.options & BlockEncoderOptions.DontFixBranches) == BlockEncoderOptions.None;
			}
		}

		// Token: 0x17000761 RID: 1889
		// (get) Token: 0x060020F4 RID: 8436 RVA: 0x00068568 File Offset: 0x00066768
		private bool ReturnRelocInfos
		{
			get
			{
				return (this.options & BlockEncoderOptions.ReturnRelocInfos) > BlockEncoderOptions.None;
			}
		}

		// Token: 0x17000762 RID: 1890
		// (get) Token: 0x060020F5 RID: 8437 RVA: 0x00068575 File Offset: 0x00066775
		private bool ReturnNewInstructionOffsets
		{
			get
			{
				return (this.options & BlockEncoderOptions.ReturnNewInstructionOffsets) > BlockEncoderOptions.None;
			}
		}

		// Token: 0x17000763 RID: 1891
		// (get) Token: 0x060020F6 RID: 8438 RVA: 0x00068582 File Offset: 0x00066782
		private bool ReturnConstantOffsets
		{
			get
			{
				return (this.options & BlockEncoderOptions.ReturnConstantOffsets) > BlockEncoderOptions.None;
			}
		}

		// Token: 0x060020F7 RID: 8439 RVA: 0x00068590 File Offset: 0x00066790
		private BlockEncoder(int bitness, InstructionBlock[] instrBlocks, BlockEncoderOptions options)
		{
			if (bitness != 16 && bitness != 32 && bitness != 64)
			{
				throw new ArgumentOutOfRangeException("bitness");
			}
			if (instrBlocks == null)
			{
				throw new ArgumentNullException("instrBlocks");
			}
			this.bitness = bitness;
			this.nullEncoder = Encoder.Create(bitness, BlockEncoder.NullCodeWriter.Instance);
			this.options = options;
			this.blocks = new Block[instrBlocks.Length];
			int instrCount = 0;
			for (int i = 0; i < instrBlocks.Length; i++)
			{
				IList<Instruction> instructions = instrBlocks[i].Instructions;
				if (instructions == null)
				{
					throw new ArgumentException();
				}
				Block block = new Block(this, instrBlocks[i].CodeWriter, instrBlocks[i].RIP, this.ReturnRelocInfos ? new List<RelocInfo>() : null);
				this.blocks[i] = block;
				Instr[] instrs = new Instr[instructions.Count];
				ulong ip = instrBlocks[i].RIP;
				for (int j = 0; j < instrs.Length; j++)
				{
					Instruction instruction = instructions[j];
					Instr instr = Instr.Create(this, block, instruction);
					instr.IP = ip;
					instrs[j] = instr;
					instrCount++;
					ip += (ulong)instr.Size;
				}
				block.SetInstructions(instrs);
			}
			Array.Sort<Block>(this.blocks, (Block a, Block b) => a.RIP.CompareTo(b.RIP));
			Dictionary<ulong, Instr> toInstr = new Dictionary<ulong, Instr>(instrCount);
			this.toInstr = toInstr;
			bool hasMultipleZeroIPInstrs = false;
			Block[] array = this.blocks;
			for (int k = 0; k < array.Length; k++)
			{
				foreach (Instr instr2 in array[k].Instructions)
				{
					ulong origIP = instr2.OrigIP;
					Instr instr4;
					if (toInstr.TryGetValue(origIP, out instr4))
					{
						if (origIP != 0UL)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 1);
							defaultInterpolatedStringHandler.AppendLiteral("Multiple instructions with the same IP: 0x");
							defaultInterpolatedStringHandler.AppendFormatted<ulong>(origIP, "X");
							throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
						}
						hasMultipleZeroIPInstrs = true;
					}
					else
					{
						toInstr[origIP] = instr2;
					}
				}
			}
			if (hasMultipleZeroIPInstrs)
			{
				toInstr.Remove(0UL);
			}
			foreach (Block block2 in this.blocks)
			{
				ulong ip2 = block2.RIP;
				foreach (Instr instr3 in block2.Instructions)
				{
					instr3.IP = ip2;
					if (!instr3.Done)
					{
						instr3.Initialize(this);
					}
					ip2 += (ulong)instr3.Size;
				}
			}
		}

		// Token: 0x060020F8 RID: 8440 RVA: 0x00068828 File Offset: 0x00066A28
		public static bool TryEncode(int bitness, InstructionBlock block, [<b37590d4-39fb-478a-88de-d293f3364852>NotNullWhen(false)] out string errorMessage, out BlockEncoderResult result, BlockEncoderOptions options = BlockEncoderOptions.None)
		{
			BlockEncoderResult[] resultArray;
			if (BlockEncoder.TryEncode(bitness, new InstructionBlock[] { block }, out errorMessage, out resultArray, options))
			{
				result = resultArray[0];
				return true;
			}
			result = default(BlockEncoderResult);
			return false;
		}

		// Token: 0x060020F9 RID: 8441 RVA: 0x00068867 File Offset: 0x00066A67
		public static bool TryEncode(int bitness, [Nullable(1)] InstructionBlock[] blocks, [<b37590d4-39fb-478a-88de-d293f3364852>NotNullWhen(false)] out string errorMessage, [<b37590d4-39fb-478a-88de-d293f3364852>NotNullWhen(true)] out BlockEncoderResult[] result, BlockEncoderOptions options = BlockEncoderOptions.None)
		{
			return new BlockEncoder(bitness, blocks, options).Encode(out errorMessage, out result);
		}

		// Token: 0x060020FA RID: 8442 RVA: 0x0006887C File Offset: 0x00066A7C
		private bool Encode([<b37590d4-39fb-478a-88de-d293f3364852>NotNullWhen(false)] out string errorMessage, [<b37590d4-39fb-478a-88de-d293f3364852>NotNullWhen(true)] out BlockEncoderResult[] result)
		{
			for (int iter = 0; iter < 5; iter++)
			{
				bool updated = false;
				foreach (Block block2 in this.blocks)
				{
					ulong ip = block2.RIP;
					ulong gained = 0UL;
					foreach (Instr instr in block2.Instructions)
					{
						instr.IP = ip;
						if (!instr.Done)
						{
							uint oldSize = instr.Size;
							if (instr.Optimize(gained))
							{
								if (instr.Size > oldSize)
								{
									errorMessage = "Internal error: new size > old size";
									result = null;
									return false;
								}
								if (instr.Size < oldSize)
								{
									gained += (ulong)(oldSize - instr.Size);
									updated = true;
								}
							}
							else if (instr.Size != oldSize)
							{
								errorMessage = "Internal error: new size != old size";
								result = null;
								return false;
							}
						}
						ip += (ulong)instr.Size;
					}
				}
				if (!updated)
				{
					break;
				}
			}
			Block[] array = this.blocks;
			for (int k = 0; k < array.Length; k++)
			{
				array[k].InitializeData();
			}
			BlockEncoderResult[] resultArray = new BlockEncoderResult[this.blocks.Length];
			for (int i = 0; i < this.blocks.Length; i++)
			{
				Block block = this.blocks[i];
				Encoder encoder = Encoder.Create(this.bitness, block.CodeWriter);
				ulong ip2 = block.RIP;
				uint[] newInstructionOffsets = (this.ReturnNewInstructionOffsets ? new uint[block.Instructions.Length] : null);
				ConstantOffsets[] constantOffsets = (this.ReturnConstantOffsets ? new ConstantOffsets[block.Instructions.Length] : null);
				Instr[] instructions = block.Instructions;
				for (int j = 0; j < instructions.Length; j++)
				{
					Instr instr2 = instructions[j];
					uint bytesWritten = block.CodeWriter.BytesWritten;
					bool isOriginalInstruction;
					if (constantOffsets != null)
					{
						errorMessage = instr2.TryEncode(encoder, out constantOffsets[j], out isOriginalInstruction);
					}
					else
					{
						ConstantOffsets constantOffsets2;
						errorMessage = instr2.TryEncode(encoder, out constantOffsets2, out isOriginalInstruction);
					}
					if (errorMessage != null)
					{
						result = null;
						return false;
					}
					uint size = block.CodeWriter.BytesWritten - bytesWritten;
					if (size != instr2.Size)
					{
						errorMessage = "Internal error: didn't write all bytes";
						result = null;
						return false;
					}
					if (newInstructionOffsets != null)
					{
						if (isOriginalInstruction)
						{
							newInstructionOffsets[j] = (uint)(ip2 - block.RIP);
						}
						else
						{
							newInstructionOffsets[j] = uint.MaxValue;
						}
					}
					ip2 += (ulong)size;
				}
				resultArray[i] = new BlockEncoderResult(block.RIP, block.relocInfos, newInstructionOffsets, constantOffsets);
				block.WriteData();
			}
			errorMessage = null;
			result = resultArray;
			return true;
		}

		// Token: 0x060020FB RID: 8443 RVA: 0x00068B0C File Offset: 0x00066D0C
		internal TargetInstr GetTarget(ulong address)
		{
			Instr instr;
			if (this.toInstr.TryGetValue(address, out instr))
			{
				return new TargetInstr(instr);
			}
			return new TargetInstr(address);
		}

		// Token: 0x060020FC RID: 8444 RVA: 0x00068B38 File Offset: 0x00066D38
		internal uint GetInstructionSize(in Instruction instruction, ulong ip)
		{
			uint size;
			string text;
			if (!this.nullEncoder.TryEncode(instruction, ip, out size, out text))
			{
				size = 15U;
			}
			return size;
		}

		// Token: 0x0400168D RID: 5773
		private readonly int bitness;

		// Token: 0x0400168E RID: 5774
		private readonly BlockEncoderOptions options;

		// Token: 0x0400168F RID: 5775
		private readonly Block[] blocks;

		// Token: 0x04001690 RID: 5776
		private readonly Encoder nullEncoder;

		// Token: 0x04001691 RID: 5777
		private readonly Dictionary<ulong, Instr> toInstr;

		// Token: 0x0200062F RID: 1583
		private sealed class NullCodeWriter : CodeWriter
		{
			// Token: 0x060020FD RID: 8445 RVA: 0x00065D0F File Offset: 0x00063F0F
			private NullCodeWriter()
			{
			}

			// Token: 0x060020FE RID: 8446 RVA: 0x0001B842 File Offset: 0x00019A42
			public override void WriteByte(byte value)
			{
			}

			// Token: 0x04001692 RID: 5778
			public static readonly BlockEncoder.NullCodeWriter Instance = new BlockEncoder.NullCodeWriter();
		}
	}
}
