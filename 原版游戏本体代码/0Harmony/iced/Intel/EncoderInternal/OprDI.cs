using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000691 RID: 1681
	internal sealed class OprDI : Op
	{
		// Token: 0x060023F5 RID: 9205 RVA: 0x00073FB4 File Offset: 0x000721B4
		private static int GetRegSize(OpKind opKind)
		{
			if (opKind == OpKind.MemorySegRDI)
			{
				return 8;
			}
			if (opKind == OpKind.MemorySegEDI)
			{
				return 4;
			}
			if (opKind == OpKind.MemorySegDI)
			{
				return 2;
			}
			return 0;
		}

		// Token: 0x060023F6 RID: 9206 RVA: 0x00073FCC File Offset: 0x000721CC
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			int regSize = OprDI.GetRegSize(instruction.GetOpKind(operand));
			if (regSize == 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(": expected OpKind = ");
				defaultInterpolatedStringHandler.AppendFormatted("MemorySegDI");
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted("MemorySegEDI");
				defaultInterpolatedStringHandler.AppendLiteral(" or ");
				defaultInterpolatedStringHandler.AppendFormatted("MemorySegRDI");
				encoder.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				return;
			}
			encoder.SetAddrSize(regSize);
		}
	}
}
