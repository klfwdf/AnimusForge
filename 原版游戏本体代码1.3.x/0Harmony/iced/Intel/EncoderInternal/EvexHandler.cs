using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000676 RID: 1654
	internal sealed class EvexHandler : OpCodeHandler
	{
		// Token: 0x060023D4 RID: 9172 RVA: 0x000737FC File Offset: 0x000719FC
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
					OpHandlerData.EvexOps[op0 - 1],
					OpHandlerData.EvexOps[op - 1],
					OpHandlerData.EvexOps[op2 - 1],
					OpHandlerData.EvexOps[op3 - 1]
				};
			}
			if (op2 != 0)
			{
				return new Op[]
				{
					OpHandlerData.EvexOps[op0 - 1],
					OpHandlerData.EvexOps[op - 1],
					OpHandlerData.EvexOps[op2 - 1]
				};
			}
			if (op != 0)
			{
				return new Op[]
				{
					OpHandlerData.EvexOps[op0 - 1],
					OpHandlerData.EvexOps[op - 1]
				};
			}
			if (op0 != 0)
			{
				return new Op[] { OpHandlerData.EvexOps[op0 - 1] };
			}
			return Array2.Empty<Op>();
		}

		// Token: 0x060023D5 RID: 9173 RVA: 0x000738CC File Offset: 0x00071ACC
		public EvexHandler(EncFlags1 encFlags1, EncFlags2 encFlags2, EncFlags3 encFlags3)
			: base(encFlags2, encFlags3, false, EvexHandler.tryConvertToDisp8N, EvexHandler.CreateOps(encFlags1))
		{
			this.tupleType = (TupleType)((encFlags3 >> 7) & EncFlags3.TupleTypeMask);
			this.table = (uint)((encFlags2 >> 17) & EncFlags2.TableMask);
			this.p1Bits = (uint)((EncFlags2)4U | ((encFlags2 >> 20) & EncFlags2.MandatoryPrefixMask));
			this.wbit = (WBit)((encFlags2 >> 22) & EncFlags2.MandatoryPrefixMask);
			if (this.wbit == WBit.W1)
			{
				this.p1Bits |= 128U;
			}
			switch ((encFlags2 >> 24) & EncFlags2.TableMask)
			{
			case EncFlags2.None:
			case EncFlags2.MandatoryPrefixMask:
			case (EncFlags2)4U:
				this.llBits = 0U;
				break;
			case (EncFlags2)1U:
			case (EncFlags2)5U:
				this.llBits = 32U;
				break;
			case (EncFlags2)2U:
				this.llBits = 0U;
				this.mask_LL = 96U;
				break;
			case (EncFlags2)6U:
				this.llBits = 64U;
				break;
			default:
				throw new InvalidOperationException();
			}
			if (this.wbit == WBit.WIG)
			{
				this.mask_W |= 128U;
			}
		}

		// Token: 0x060023D6 RID: 9174 RVA: 0x000739B4 File Offset: 0x00071BB4
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction)
		{
			encoder.WritePrefixes(instruction, true);
			uint encoderFlags = (uint)encoder.EncoderFlags;
			encoder.WriteByteInternal(98U);
			uint b = this.table;
			b |= (encoderFlags & 7U) << 5;
			b |= (encoderFlags >> 5) & 16U;
			b ^= 4294967280U;
			encoder.WriteByteInternal(b);
			b = this.p1Bits;
			b |= (~encoderFlags >> 24) & 120U;
			b |= this.mask_W & encoder.Internal_EVEX_WIG;
			encoder.WriteByteInternal(b);
			b = instruction.InternalOpMask;
			if (b != 0U)
			{
				if ((this.EncFlags3 & EncFlags3.OpMaskRegister) == EncFlags3.None)
				{
					encoder.ErrorMessage = "The instruction doesn't support opmask registers";
				}
			}
			else if ((this.EncFlags3 & (EncFlags3)2147483648U) != EncFlags3.None)
			{
				encoder.ErrorMessage = "The instruction must use an opmask register";
			}
			b |= (encoderFlags >> 28) & 8U;
			if (instruction.SuppressAllExceptions)
			{
				if ((this.EncFlags3 & EncFlags3.SuppressAllExceptions) == EncFlags3.None)
				{
					encoder.ErrorMessage = "The instruction doesn't support suppress-all-exceptions";
				}
				b |= 16U;
			}
			RoundingControl rc = instruction.RoundingControl;
			if (rc != RoundingControl.None)
			{
				if ((this.EncFlags3 & EncFlags3.RoundingControl) == EncFlags3.None)
				{
					encoder.ErrorMessage = "The instruction doesn't support rounding control";
				}
				b |= 16U;
				b |= (uint)((uint)(rc - RoundingControl.RoundToNearest) << 5);
			}
			else if ((this.EncFlags3 & EncFlags3.SuppressAllExceptions) == EncFlags3.None || !instruction.SuppressAllExceptions)
			{
				b |= this.llBits;
			}
			if ((encoderFlags & 1024U) != 0U)
			{
				b |= 16U;
			}
			else if (instruction.IsBroadcast)
			{
				encoder.ErrorMessage = "The instruction doesn't support broadcasting";
			}
			if (instruction.ZeroingMasking)
			{
				if ((this.EncFlags3 & EncFlags3.ZeroingMasking) == EncFlags3.None)
				{
					encoder.ErrorMessage = "The instruction doesn't support zeroing masking";
				}
				b |= 128U;
			}
			b ^= 8U;
			b |= this.mask_LL & encoder.Internal_EVEX_LIG;
			encoder.WriteByteInternal(b);
		}

		// Token: 0x0400346C RID: 13420
		private readonly WBit wbit;

		// Token: 0x0400346D RID: 13421
		private readonly TupleType tupleType;

		// Token: 0x0400346E RID: 13422
		private readonly uint table;

		// Token: 0x0400346F RID: 13423
		private readonly uint p1Bits;

		// Token: 0x04003470 RID: 13424
		private readonly uint llBits;

		// Token: 0x04003471 RID: 13425
		private readonly uint mask_W;

		// Token: 0x04003472 RID: 13426
		private readonly uint mask_LL;

		// Token: 0x04003473 RID: 13427
		private static readonly TryConvertToDisp8N tryConvertToDisp8N = new TryConvertToDisp8N(EvexHandler.TryConvertToDisp8NImpl.TryConvertToDisp8N);

		// Token: 0x02000677 RID: 1655
		private sealed class TryConvertToDisp8NImpl
		{
			// Token: 0x060023D8 RID: 9176 RVA: 0x00073B60 File Offset: 0x00071D60
			public static bool TryConvertToDisp8N(Encoder encoder, OpCodeHandler handler, in Instruction instruction, int displ, out sbyte compressedValue)
			{
				int i = (int)TupleTypeTable.GetDisp8N(((EvexHandler)handler).tupleType, (encoder.EncoderFlags & EncoderFlags.Broadcast) > EncoderFlags.None);
				int res = displ / i;
				if (res * i == displ && -128 <= res && res <= 127)
				{
					compressedValue = (sbyte)res;
					return true;
				}
				compressedValue = 0;
				return false;
			}
		}
	}
}
