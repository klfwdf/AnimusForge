using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000697 RID: 1687
	internal sealed class OpX : Op
	{
		// Token: 0x06002407 RID: 9223 RVA: 0x00074263 File Offset: 0x00072463
		internal static int GetXRegSize(OpKind opKind)
		{
			if (opKind == OpKind.MemorySegRSI)
			{
				return 8;
			}
			if (opKind == OpKind.MemorySegESI)
			{
				return 4;
			}
			if (opKind == OpKind.MemorySegSI)
			{
				return 2;
			}
			return 0;
		}

		// Token: 0x06002408 RID: 9224 RVA: 0x0007427B File Offset: 0x0007247B
		internal static int GetYRegSize(OpKind opKind)
		{
			if (opKind == OpKind.MemoryESRDI)
			{
				return 8;
			}
			if (opKind == OpKind.MemoryESEDI)
			{
				return 4;
			}
			if (opKind == OpKind.MemoryESDI)
			{
				return 2;
			}
			return 0;
		}

		// Token: 0x06002409 RID: 9225 RVA: 0x00074294 File Offset: 0x00072494
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			int regXSize = OpX.GetXRegSize(instruction.GetOpKind(operand));
			if (regXSize == 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(": expected OpKind = ");
				defaultInterpolatedStringHandler.AppendFormatted("MemorySegSI");
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted("MemorySegESI");
				defaultInterpolatedStringHandler.AppendLiteral(" or ");
				defaultInterpolatedStringHandler.AppendFormatted("MemorySegRSI");
				encoder.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				return;
			}
			Code code = instruction.Code;
			if (code - Code.Movsb_m8_m8 <= 3)
			{
				int regYSize = OpX.GetYRegSize(instruction.Op0Kind);
				if (regXSize != regYSize)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(64, 2);
					defaultInterpolatedStringHandler2.AppendLiteral("Same sized register must be used: reg #1 size = ");
					defaultInterpolatedStringHandler2.AppendFormatted<int>(regYSize * 8);
					defaultInterpolatedStringHandler2.AppendLiteral(", reg #2 size = ");
					defaultInterpolatedStringHandler2.AppendFormatted<int>(regXSize * 8);
					encoder.ErrorMessage = defaultInterpolatedStringHandler2.ToStringAndClear();
					return;
				}
			}
			encoder.SetAddrSize(regXSize);
		}
	}
}
