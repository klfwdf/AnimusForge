using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000675 RID: 1653
	internal sealed class XopHandler : OpCodeHandler
	{
		// Token: 0x060023D1 RID: 9169 RVA: 0x00073664 File Offset: 0x00071864
		private static Op[] CreateOps(EncFlags1 encFlags1)
		{
			int op0 = (int)(encFlags1 & EncFlags1.XOP_OpMask);
			int op = (int)((encFlags1 >> 5) & EncFlags1.XOP_OpMask);
			int op2 = (int)((encFlags1 >> 10) & EncFlags1.XOP_OpMask);
			int op3 = (int)((encFlags1 >> 15) & EncFlags1.XOP_OpMask);
			if (op3 != 0)
			{
				return new Op[]
				{
					OpHandlerData.XopOps[op0 - 1],
					OpHandlerData.XopOps[op - 1],
					OpHandlerData.XopOps[op2 - 1],
					OpHandlerData.XopOps[op3 - 1]
				};
			}
			if (op2 != 0)
			{
				return new Op[]
				{
					OpHandlerData.XopOps[op0 - 1],
					OpHandlerData.XopOps[op - 1],
					OpHandlerData.XopOps[op2 - 1]
				};
			}
			if (op != 0)
			{
				return new Op[]
				{
					OpHandlerData.XopOps[op0 - 1],
					OpHandlerData.XopOps[op - 1]
				};
			}
			if (op0 != 0)
			{
				return new Op[] { OpHandlerData.XopOps[op0 - 1] };
			}
			return Array2.Empty<Op>();
		}

		// Token: 0x060023D2 RID: 9170 RVA: 0x00073734 File Offset: 0x00071934
		public XopHandler(EncFlags1 encFlags1, EncFlags2 encFlags2, EncFlags3 encFlags3)
			: base(encFlags2, encFlags3, false, null, XopHandler.CreateOps(encFlags1))
		{
			this.table = (uint)(8U + ((encFlags2 >> 17) & EncFlags2.TableMask));
			LBit lbit = (LBit)((encFlags2 >> 24) & EncFlags2.TableMask);
			if (lbit == LBit.L1 || lbit == LBit.L256)
			{
				this.lastByte = 4U;
			}
			if (((encFlags2 >> 22) & EncFlags2.MandatoryPrefixMask) == (EncFlags2)1U)
			{
				this.lastByte |= 128U;
			}
			this.lastByte |= (uint)((encFlags2 >> 20) & EncFlags2.MandatoryPrefixMask);
		}

		// Token: 0x060023D3 RID: 9171 RVA: 0x000737A4 File Offset: 0x000719A4
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction)
		{
			encoder.WritePrefixes(instruction, true);
			encoder.WriteByteInternal(143U);
			uint encoderFlags = (uint)encoder.EncoderFlags;
			uint b = this.table;
			b |= (~encoderFlags & 7U) << 5;
			encoder.WriteByteInternal(b);
			b = this.lastByte;
			b |= (~encoderFlags >> 24) & 120U;
			encoder.WriteByteInternal(b);
		}

		// Token: 0x0400346A RID: 13418
		private readonly uint table;

		// Token: 0x0400346B RID: 13419
		private readonly uint lastByte;
	}
}
