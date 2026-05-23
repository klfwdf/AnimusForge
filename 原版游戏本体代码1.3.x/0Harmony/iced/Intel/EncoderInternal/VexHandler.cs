using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000674 RID: 1652
	internal sealed class VexHandler : OpCodeHandler
	{
		// Token: 0x060023CE RID: 9166 RVA: 0x000733DC File Offset: 0x000715DC
		private static Op[] CreateOps(EncFlags1 encFlags1)
		{
			int op0 = (int)(encFlags1 & EncFlags1.VEX_OpMask);
			int op = (int)((encFlags1 >> 6) & EncFlags1.VEX_OpMask);
			int op2 = (int)((encFlags1 >> 12) & EncFlags1.VEX_OpMask);
			int op3 = (int)((encFlags1 >> 18) & EncFlags1.VEX_OpMask);
			int op4 = (int)((encFlags1 >> 24) & EncFlags1.VEX_OpMask);
			if (op4 != 0)
			{
				return new Op[]
				{
					OpHandlerData.VexOps[op0 - 1],
					OpHandlerData.VexOps[op - 1],
					OpHandlerData.VexOps[op2 - 1],
					OpHandlerData.VexOps[op3 - 1],
					OpHandlerData.VexOps[op4 - 1]
				};
			}
			if (op3 != 0)
			{
				return new Op[]
				{
					OpHandlerData.VexOps[op0 - 1],
					OpHandlerData.VexOps[op - 1],
					OpHandlerData.VexOps[op2 - 1],
					OpHandlerData.VexOps[op3 - 1]
				};
			}
			if (op2 != 0)
			{
				return new Op[]
				{
					OpHandlerData.VexOps[op0 - 1],
					OpHandlerData.VexOps[op - 1],
					OpHandlerData.VexOps[op2 - 1]
				};
			}
			if (op != 0)
			{
				return new Op[]
				{
					OpHandlerData.VexOps[op0 - 1],
					OpHandlerData.VexOps[op - 1]
				};
			}
			if (op0 != 0)
			{
				return new Op[] { OpHandlerData.VexOps[op0 - 1] };
			}
			return Array2.Empty<Op>();
		}

		// Token: 0x060023CF RID: 9167 RVA: 0x000734FC File Offset: 0x000716FC
		public VexHandler(EncFlags1 encFlags1, EncFlags2 encFlags2, EncFlags3 encFlags3)
			: base(encFlags2, encFlags3, false, null, VexHandler.CreateOps(encFlags1))
		{
			this.table = (uint)((encFlags2 >> 17) & EncFlags2.TableMask);
			WBit wbit = (WBit)((encFlags2 >> 22) & EncFlags2.MandatoryPrefixMask);
			this.W1 = ((wbit == WBit.W1) ? uint.MaxValue : 0U);
			LBit lbit = (LBit)((encFlags2 >> 24) & EncFlags2.TableMask);
			if (lbit == LBit.L1 || lbit == LBit.L256)
			{
				this.lastByte = 4U;
			}
			if (this.W1 != 0U)
			{
				this.lastByte |= 128U;
			}
			this.lastByte |= (uint)((encFlags2 >> 20) & EncFlags2.MandatoryPrefixMask);
			if (wbit == WBit.WIG)
			{
				this.mask_W_L |= 128U;
			}
			if (lbit == LBit.LIG)
			{
				this.mask_W_L |= 4U;
				this.mask_L |= 4U;
			}
		}

		// Token: 0x060023D0 RID: 9168 RVA: 0x000735B4 File Offset: 0x000717B4
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction)
		{
			encoder.WritePrefixes(instruction, true);
			uint encoderFlags = (uint)encoder.EncoderFlags;
			uint b = this.lastByte;
			b |= (~encoderFlags >> 24) & 120U;
			if ((encoder.Internal_PreventVEX2 | this.W1 | (this.table - 1U) | (encoderFlags & 11U)) != 0U)
			{
				encoder.WriteByteInternal(196U);
				uint b2 = this.table;
				b2 |= (~encoderFlags & 7U) << 5;
				encoder.WriteByteInternal(b2);
				b |= this.mask_W_L & encoder.Internal_VEX_WIG_LIG;
				encoder.WriteByteInternal(b);
				return;
			}
			encoder.WriteByteInternal(197U);
			b |= (~encoderFlags & 4U) << 5;
			b |= this.mask_L & encoder.Internal_VEX_LIG;
			encoder.WriteByteInternal(b);
		}

		// Token: 0x04003465 RID: 13413
		private readonly uint table;

		// Token: 0x04003466 RID: 13414
		private readonly uint lastByte;

		// Token: 0x04003467 RID: 13415
		private readonly uint mask_W_L;

		// Token: 0x04003468 RID: 13416
		private readonly uint mask_L;

		// Token: 0x04003469 RID: 13417
		private readonly uint W1;
	}
}
