using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000696 RID: 1686
	internal sealed class OpI4 : Op
	{
		// Token: 0x06002404 RID: 9220 RVA: 0x000741D4 File Offset: 0x000723D4
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			OpKind opImmKind = instruction.GetOpKind(operand);
			if (!encoder.Verify(operand, OpKind.Immediate8, opImmKind))
			{
				return;
			}
			if (instruction.Immediate8 > 15)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(": Immediate value must be 0-15, but value is 0x");
				defaultInterpolatedStringHandler.AppendFormatted<byte>(instruction.Immediate8, "X2");
				encoder.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				return;
			}
			encoder.ImmSize = ImmSize.Size1;
			encoder.Immediate |= (uint)instruction.Immediate8;
		}

		// Token: 0x06002405 RID: 9221 RVA: 0x000413EB File Offset: 0x0003F5EB
		public override OpKind GetImmediateOpKind()
		{
			return OpKind.Immediate8;
		}
	}
}
