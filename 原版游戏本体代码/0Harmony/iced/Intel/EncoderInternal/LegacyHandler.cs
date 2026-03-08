using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000673 RID: 1651
	internal sealed class LegacyHandler : OpCodeHandler
	{
		// Token: 0x060023CB RID: 9163 RVA: 0x000731B8 File Offset: 0x000713B8
		private static Op[] CreateOps(EncFlags1 encFlags1)
		{
			int op0 = (int)(encFlags1 & EncFlags1.Legacy_OpMask);
			int op = (int)((encFlags1 >> 7) & EncFlags1.Legacy_OpMask);
			int op2 = (int)((encFlags1 >> 14) & EncFlags1.Legacy_OpMask);
			int op3 = (int)((encFlags1 >> 21) & EncFlags1.Legacy_OpMask);
			if (op3 != 0)
			{
				return new Op[]
				{
					OpHandlerData.LegacyOps[op0 - 1],
					OpHandlerData.LegacyOps[op - 1],
					OpHandlerData.LegacyOps[op2 - 1],
					OpHandlerData.LegacyOps[op3 - 1]
				};
			}
			if (op2 != 0)
			{
				return new Op[]
				{
					OpHandlerData.LegacyOps[op0 - 1],
					OpHandlerData.LegacyOps[op - 1],
					OpHandlerData.LegacyOps[op2 - 1]
				};
			}
			if (op != 0)
			{
				return new Op[]
				{
					OpHandlerData.LegacyOps[op0 - 1],
					OpHandlerData.LegacyOps[op - 1]
				};
			}
			if (op0 != 0)
			{
				return new Op[] { OpHandlerData.LegacyOps[op0 - 1] };
			}
			return Array2.Empty<Op>();
		}

		// Token: 0x060023CC RID: 9164 RVA: 0x00073288 File Offset: 0x00071488
		public LegacyHandler(EncFlags1 encFlags1, EncFlags2 encFlags2, EncFlags3 encFlags3)
			: base(encFlags2, encFlags3, false, null, LegacyHandler.CreateOps(encFlags1))
		{
			switch ((encFlags2 >> 17) & EncFlags2.TableMask)
			{
			case EncFlags2.None:
				this.tableByte1 = 0U;
				this.tableByte2 = 0U;
				break;
			case (EncFlags2)1U:
				this.tableByte1 = 15U;
				this.tableByte2 = 0U;
				break;
			case (EncFlags2)2U:
				this.tableByte1 = 15U;
				this.tableByte2 = 56U;
				break;
			case EncFlags2.MandatoryPrefixMask:
				this.tableByte1 = 15U;
				this.tableByte2 = 58U;
				break;
			default:
				throw new InvalidOperationException();
			}
			uint num;
			switch ((encFlags2 >> 20) & EncFlags2.MandatoryPrefixMask)
			{
			case EncFlags2.None:
				num = 0U;
				break;
			case (EncFlags2)1U:
				num = 102U;
				break;
			case (EncFlags2)2U:
				num = 243U;
				break;
			case EncFlags2.MandatoryPrefixMask:
				num = 242U;
				break;
			default:
				throw new InvalidOperationException();
			}
			this.mandatoryPrefix = num;
		}

		// Token: 0x060023CD RID: 9165 RVA: 0x00073354 File Offset: 0x00071554
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction)
		{
			uint b = this.mandatoryPrefix;
			encoder.WritePrefixes(instruction, b != 243U);
			if (b != 0U)
			{
				encoder.WriteByteInternal(b);
			}
			b = (uint)encoder.EncoderFlags;
			b &= 79U;
			if (b != 0U)
			{
				if ((encoder.EncoderFlags & EncoderFlags.HighLegacy8BitRegs) != EncoderFlags.None)
				{
					encoder.ErrorMessage = "Registers AH, CH, DH, BH can't be used if there's a REX prefix. Use AL, CL, DL, BL, SPL, BPL, SIL, DIL, R8L-R15L instead.";
				}
				b |= 64U;
				encoder.WriteByteInternal(b);
			}
			if ((b = this.tableByte1) != 0U)
			{
				encoder.WriteByteInternal(b);
				if ((b = this.tableByte2) != 0U)
				{
					encoder.WriteByteInternal(b);
				}
			}
		}

		// Token: 0x04003462 RID: 13410
		private readonly uint tableByte1;

		// Token: 0x04003463 RID: 13411
		private readonly uint tableByte2;

		// Token: 0x04003464 RID: 13412
		private readonly uint mandatoryPrefix;
	}
}
