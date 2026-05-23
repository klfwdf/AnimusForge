using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000698 RID: 1688
	internal sealed class OpY : Op
	{
		// Token: 0x0600240B RID: 9227 RVA: 0x00074394 File Offset: 0x00072594
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			int regYSize = OpX.GetYRegSize(instruction.GetOpKind(operand));
			if (regYSize == 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(": expected OpKind = ");
				defaultInterpolatedStringHandler.AppendFormatted("MemoryESDI");
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted("MemoryESEDI");
				defaultInterpolatedStringHandler.AppendLiteral(" or ");
				defaultInterpolatedStringHandler.AppendFormatted("MemoryESRDI");
				encoder.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				return;
			}
			Code code = instruction.Code;
			if (code - Code.Cmpsb_m8_m8 <= 3)
			{
				int regXSize = OpX.GetXRegSize(instruction.Op0Kind);
				if (regXSize != regYSize)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(64, 2);
					defaultInterpolatedStringHandler2.AppendLiteral("Same sized register must be used: reg #1 size = ");
					defaultInterpolatedStringHandler2.AppendFormatted<int>(regXSize * 8);
					defaultInterpolatedStringHandler2.AppendLiteral(", reg #2 size = ");
					defaultInterpolatedStringHandler2.AppendFormatted<int>(regYSize * 8);
					encoder.ErrorMessage = defaultInterpolatedStringHandler2.ToStringAndClear();
					return;
				}
			}
			encoder.SetAddrSize(regYSize);
		}
	}
}
