using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000692 RID: 1682
	internal sealed class OpIb : Op
	{
		// Token: 0x060023F8 RID: 9208 RVA: 0x00074064 File Offset: 0x00072264
		public OpIb(OpKind opKind)
		{
			this.opKind = opKind;
		}

		// Token: 0x060023F9 RID: 9209 RVA: 0x00074074 File Offset: 0x00072274
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			ImmSize immSize = encoder.ImmSize;
			if (immSize != ImmSize.Size1)
			{
				if (immSize != ImmSize.Size2)
				{
					OpKind opImmKind = instruction.GetOpKind(operand);
					if (!encoder.Verify(operand, this.opKind, opImmKind))
					{
						return;
					}
					encoder.ImmSize = ImmSize.Size1;
					encoder.Immediate = (uint)instruction.Immediate8;
					return;
				}
				else
				{
					if (!encoder.Verify(operand, OpKind.Immediate8_2nd, instruction.GetOpKind(operand)))
					{
						return;
					}
					encoder.ImmSize = ImmSize.Size2_1;
					encoder.ImmediateHi = (uint)instruction.Immediate8_2nd;
					return;
				}
			}
			else
			{
				if (!encoder.Verify(operand, OpKind.Immediate8_2nd, instruction.GetOpKind(operand)))
				{
					return;
				}
				encoder.ImmSize = ImmSize.Size1_1;
				encoder.ImmediateHi = (uint)instruction.Immediate8_2nd;
				return;
			}
		}

		// Token: 0x060023FA RID: 9210 RVA: 0x0007410A File Offset: 0x0007230A
		public override OpKind GetImmediateOpKind()
		{
			return this.opKind;
		}

		// Token: 0x04003526 RID: 13606
		private readonly OpKind opKind;
	}
}
