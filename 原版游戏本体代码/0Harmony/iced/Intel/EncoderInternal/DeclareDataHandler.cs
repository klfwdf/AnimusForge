using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000671 RID: 1649
	internal sealed class DeclareDataHandler : OpCodeHandler
	{
		// Token: 0x060023C7 RID: 9159 RVA: 0x000730B0 File Offset: 0x000712B0
		public DeclareDataHandler(Code code)
			: base(EncFlags2.None, EncFlags3.Bit16or32 | EncFlags3.Bit64, true, null, Array2.Empty<Op>())
		{
			int num;
			switch (code)
			{
			case Code.DeclareByte:
				num = 1;
				break;
			case Code.DeclareWord:
				num = 2;
				break;
			case Code.DeclareDword:
				num = 4;
				break;
			case Code.DeclareQword:
				num = 8;
				break;
			default:
				throw new InvalidOperationException();
			}
			this.elemLength = num;
			this.maxLength = 16 / this.elemLength;
		}

		// Token: 0x060023C8 RID: 9160 RVA: 0x00073118 File Offset: 0x00071318
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction)
		{
			int declDataCount = instruction.DeclareDataCount;
			if (declDataCount < 1 || declDataCount > this.maxLength)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Invalid db/dw/dd/dq data count. Count = ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(declDataCount);
				defaultInterpolatedStringHandler.AppendLiteral(", max count = ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(this.maxLength);
				encoder.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				return;
			}
			int length = declDataCount * this.elemLength;
			for (int i = 0; i < length; i++)
			{
				encoder.WriteByteInternal((uint)instruction.GetDeclareByteValue(i));
			}
		}

		// Token: 0x04003460 RID: 13408
		private readonly int elemLength;

		// Token: 0x04003461 RID: 13409
		private readonly int maxLength;
	}
}
