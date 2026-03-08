using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200069F RID: 1695
	internal sealed class OpImm : Op
	{
		// Token: 0x0600241D RID: 9245 RVA: 0x000745FE File Offset: 0x000727FE
		public OpImm(byte value)
		{
			this.value = value;
		}

		// Token: 0x0600241E RID: 9246 RVA: 0x00074610 File Offset: 0x00072810
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			if (!encoder.Verify(operand, OpKind.Immediate8, instruction.GetOpKind(operand)))
			{
				return;
			}
			if (instruction.Immediate8 != this.value)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(": Expected 0x");
				defaultInterpolatedStringHandler.AppendFormatted<byte>(this.value, "X2");
				defaultInterpolatedStringHandler.AppendLiteral(", actual: 0x");
				defaultInterpolatedStringHandler.AppendFormatted<byte>(instruction.Immediate8, "X2");
				encoder.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				return;
			}
		}

		// Token: 0x0600241F RID: 9247 RVA: 0x000413EB File Offset: 0x0003F5EB
		public override OpKind GetImmediateOpKind()
		{
			return OpKind.Immediate8;
		}

		// Token: 0x0400352D RID: 13613
		private readonly byte value;
	}
}
