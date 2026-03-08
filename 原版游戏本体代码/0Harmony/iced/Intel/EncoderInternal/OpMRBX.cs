using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000699 RID: 1689
	internal sealed class OpMRBX : Op
	{
		// Token: 0x0600240D RID: 9229 RVA: 0x00074494 File Offset: 0x00072694
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			if (!encoder.Verify(operand, OpKind.Memory, instruction.GetOpKind(operand)))
			{
				return;
			}
			Register baseReg = instruction.MemoryBase;
			if (instruction.MemoryDisplSize != 0 || instruction.MemoryDisplacement64 != 0UL || instruction.MemoryIndexScale != 1 || instruction.MemoryIndex != Register.AL || (baseReg != Register.BX && baseReg != Register.EBX && baseReg != Register.RBX))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(56, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(": Operand must be [bx+al], [ebx+al], or [rbx+al]");
				encoder.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				return;
			}
			int regSize;
			if (baseReg == Register.RBX)
			{
				regSize = 8;
			}
			else if (baseReg == Register.EBX)
			{
				regSize = 4;
			}
			else
			{
				regSize = 2;
			}
			encoder.SetAddrSize(regSize);
		}
	}
}
