using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007E9 RID: 2025
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class SimpleInstr : Instr
	{
		// Token: 0x060026EC RID: 9964 RVA: 0x000870F9 File Offset: 0x000852F9
		public SimpleInstr(BlockEncoder blockEncoder, Block block, in Instruction instruction)
			: base(block, instruction.IP)
		{
			this.Done = true;
			this.instruction = instruction;
			this.Size = blockEncoder.GetInstructionSize(instruction, instruction.IP);
		}

		// Token: 0x060026ED RID: 9965 RVA: 0x0001B842 File Offset: 0x00019A42
		public override void Initialize(BlockEncoder blockEncoder)
		{
		}

		// Token: 0x060026EE RID: 9966 RVA: 0x0001B69F File Offset: 0x0001989F
		public override bool Optimize(ulong gained)
		{
			return false;
		}

		// Token: 0x060026EF RID: 9967 RVA: 0x00087130 File Offset: 0x00085330
		[return: Nullable(2)]
		public override string TryEncode(Encoder encoder, out ConstantOffsets constantOffsets, out bool isOriginalInstruction)
		{
			isOriginalInstruction = true;
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

		// Token: 0x040039A5 RID: 14757
		private Instruction instruction;
	}
}
