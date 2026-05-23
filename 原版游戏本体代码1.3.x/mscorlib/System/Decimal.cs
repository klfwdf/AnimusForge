using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security;

namespace System
{
	// Token: 0x020000D3 RID: 211
	[ComVisible(true)]
	[NonVersionable]
	[__DynamicallyInvokable]
	[Serializable]
	public struct Decimal : IFormattable, IComparable, IConvertible, IDeserializationCallback, IComparable<decimal>, IEquatable<decimal>
	{
		// Token: 0x06000D11 RID: 3345 RVA: 0x00027C00 File Offset: 0x00025E00
		[__DynamicallyInvokable]
		public Decimal(int value)
		{
			int num = value;
			if (num >= 0)
			{
				this.flags = 0;
			}
			else
			{
				this.flags = int.MinValue;
				num = -num;
			}
			this.lo = num;
			this.mid = 0;
			this.hi = 0;
		}

		// Token: 0x06000D12 RID: 3346 RVA: 0x00027C3F File Offset: 0x00025E3F
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public Decimal(uint value)
		{
			this.flags = 0;
			this.lo = (int)value;
			this.mid = 0;
			this.hi = 0;
		}

		// Token: 0x06000D13 RID: 3347 RVA: 0x00027C60 File Offset: 0x00025E60
		[__DynamicallyInvokable]
		public Decimal(long value)
		{
			long num = value;
			if (num >= 0L)
			{
				this.flags = 0;
			}
			else
			{
				this.flags = int.MinValue;
				num = -num;
			}
			this.lo = (int)num;
			this.mid = (int)(num >> 32);
			this.hi = 0;
		}

		// Token: 0x06000D14 RID: 3348 RVA: 0x00027CA5 File Offset: 0x00025EA5
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public Decimal(ulong value)
		{
			this.flags = 0;
			this.lo = (int)value;
			this.mid = (int)(value >> 32);
			this.hi = 0;
		}

		// Token: 0x06000D15 RID: 3349
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern Decimal(float value);

		// Token: 0x06000D16 RID: 3350
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern Decimal(double value);

		// Token: 0x06000D17 RID: 3351 RVA: 0x00027CC8 File Offset: 0x00025EC8
		internal Decimal(Currency value)
		{
			decimal num = Currency.ToDecimal(value);
			this.lo = num.lo;
			this.mid = num.mid;
			this.hi = num.hi;
			this.flags = num.flags;
		}

		// Token: 0x06000D18 RID: 3352 RVA: 0x00027D0C File Offset: 0x00025F0C
		[__DynamicallyInvokable]
		public static long ToOACurrency(decimal value)
		{
			return new Currency(value).ToOACurrency();
		}

		// Token: 0x06000D19 RID: 3353 RVA: 0x00027D27 File Offset: 0x00025F27
		[__DynamicallyInvokable]
		public static decimal FromOACurrency(long cy)
		{
			return Currency.ToDecimal(Currency.FromOACurrency(cy));
		}

		// Token: 0x06000D1A RID: 3354 RVA: 0x00027D34 File Offset: 0x00025F34
		[__DynamicallyInvokable]
		public Decimal(int[] bits)
		{
			this.lo = 0;
			this.mid = 0;
			this.hi = 0;
			this.flags = 0;
			this.SetBits(bits);
		}

		// Token: 0x06000D1B RID: 3355 RVA: 0x00027D5C File Offset: 0x00025F5C
		private void SetBits(int[] bits)
		{
			if (bits == null)
			{
				throw new ArgumentNullException("bits");
			}
			if (bits.Length == 4)
			{
				int num = bits[3];
				if ((num & 2130771967) == 0 && (num & 16711680) <= 1835008)
				{
					this.lo = bits[0];
					this.mid = bits[1];
					this.hi = bits[2];
					this.flags = num;
					return;
				}
			}
			throw new ArgumentException(Environment.GetResourceString("Arg_DecBitCtor"));
		}

		// Token: 0x06000D1C RID: 3356 RVA: 0x00027DCC File Offset: 0x00025FCC
		[__DynamicallyInvokable]
		public Decimal(int lo, int mid, int hi, bool isNegative, byte scale)
		{
			if (scale > 28)
			{
				throw new ArgumentOutOfRangeException("scale", Environment.GetResourceString("ArgumentOutOfRange_DecimalScale"));
			}
			this.lo = lo;
			this.mid = mid;
			this.hi = hi;
			this.flags = (int)scale << 16;
			if (isNegative)
			{
				this.flags |= int.MinValue;
			}
		}

		// Token: 0x06000D1D RID: 3357 RVA: 0x00027E2C File Offset: 0x0002602C
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
			try
			{
				this.SetBits(decimal.GetBits(this));
			}
			catch (ArgumentException innerException)
			{
				throw new SerializationException(Environment.GetResourceString("Overflow_Decimal"), innerException);
			}
		}

		// Token: 0x06000D1E RID: 3358 RVA: 0x00027E70 File Offset: 0x00026070
		void IDeserializationCallback.OnDeserialization(object sender)
		{
			try
			{
				this.SetBits(decimal.GetBits(this));
			}
			catch (ArgumentException innerException)
			{
				throw new SerializationException(Environment.GetResourceString("Overflow_Decimal"), innerException);
			}
		}

		// Token: 0x06000D1F RID: 3359 RVA: 0x00027EB4 File Offset: 0x000260B4
		private Decimal(int lo, int mid, int hi, int flags)
		{
			if ((flags & 2130771967) == 0 && (flags & 16711680) <= 1835008)
			{
				this.lo = lo;
				this.mid = mid;
				this.hi = hi;
				this.flags = flags;
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("Arg_DecBitCtor"));
		}

		// Token: 0x06000D20 RID: 3360 RVA: 0x00027F07 File Offset: 0x00026107
		internal static decimal Abs(decimal d)
		{
			return new decimal(d.lo, d.mid, d.hi, d.flags & int.MaxValue);
		}

		// Token: 0x06000D21 RID: 3361 RVA: 0x00027F2C File Offset: 0x0002612C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal Add(decimal d1, decimal d2)
		{
			decimal.FCallAddSub(ref d1, ref d2, 0);
			return d1;
		}

		// Token: 0x06000D22 RID: 3362
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallAddSub(ref decimal d1, ref decimal d2, byte bSign);

		// Token: 0x06000D23 RID: 3363
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallAddSubOverflowed(ref decimal d1, ref decimal d2, byte bSign, ref bool overflowed);

		// Token: 0x06000D24 RID: 3364 RVA: 0x00027F39 File Offset: 0x00026139
		[__DynamicallyInvokable]
		public static decimal Ceiling(decimal d)
		{
			return -decimal.Floor(-d);
		}

		// Token: 0x06000D25 RID: 3365 RVA: 0x00027F4B File Offset: 0x0002614B
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static int Compare(decimal d1, decimal d2)
		{
			return decimal.FCallCompare(ref d1, ref d2);
		}

		// Token: 0x06000D26 RID: 3366
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int FCallCompare(ref decimal d1, ref decimal d2);

		// Token: 0x06000D27 RID: 3367 RVA: 0x00027F58 File Offset: 0x00026158
		[SecuritySafeCritical]
		public int CompareTo(object value)
		{
			if (value == null)
			{
				return 1;
			}
			if (!(value is decimal))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDecimal"));
			}
			decimal num = (decimal)value;
			return decimal.FCallCompare(ref this, ref num);
		}

		// Token: 0x06000D28 RID: 3368 RVA: 0x00027F91 File Offset: 0x00026191
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public int CompareTo(decimal value)
		{
			return decimal.FCallCompare(ref this, ref value);
		}

		// Token: 0x06000D29 RID: 3369 RVA: 0x00027F9B File Offset: 0x0002619B
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal Divide(decimal d1, decimal d2)
		{
			decimal.FCallDivide(ref d1, ref d2);
			return d1;
		}

		// Token: 0x06000D2A RID: 3370
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallDivide(ref decimal d1, ref decimal d2);

		// Token: 0x06000D2B RID: 3371
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallDivideOverflowed(ref decimal d1, ref decimal d2, ref bool overflowed);

		// Token: 0x06000D2C RID: 3372 RVA: 0x00027FA8 File Offset: 0x000261A8
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override bool Equals(object value)
		{
			if (value is decimal)
			{
				decimal num = (decimal)value;
				return decimal.FCallCompare(ref this, ref num) == 0;
			}
			return false;
		}

		// Token: 0x06000D2D RID: 3373 RVA: 0x00027FD1 File Offset: 0x000261D1
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public bool Equals(decimal value)
		{
			return decimal.FCallCompare(ref this, ref value) == 0;
		}

		// Token: 0x06000D2E RID: 3374
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public override extern int GetHashCode();

		// Token: 0x06000D2F RID: 3375 RVA: 0x00027FDE File Offset: 0x000261DE
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static bool Equals(decimal d1, decimal d2)
		{
			return decimal.FCallCompare(ref d1, ref d2) == 0;
		}

		// Token: 0x06000D30 RID: 3376 RVA: 0x00027FEC File Offset: 0x000261EC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal Floor(decimal d)
		{
			decimal.FCallFloor(ref d);
			return d;
		}

		// Token: 0x06000D31 RID: 3377
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallFloor(ref decimal d);

		// Token: 0x06000D32 RID: 3378 RVA: 0x00027FF6 File Offset: 0x000261F6
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return Number.FormatDecimal(this, null, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06000D33 RID: 3379 RVA: 0x00028009 File Offset: 0x00026209
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(string format)
		{
			return Number.FormatDecimal(this, format, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06000D34 RID: 3380 RVA: 0x0002801C File Offset: 0x0002621C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(IFormatProvider provider)
		{
			return Number.FormatDecimal(this, null, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06000D35 RID: 3381 RVA: 0x00028030 File Offset: 0x00026230
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(string format, IFormatProvider provider)
		{
			return Number.FormatDecimal(this, format, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06000D36 RID: 3382 RVA: 0x00028044 File Offset: 0x00026244
		[__DynamicallyInvokable]
		public static decimal Parse(string s)
		{
			return Number.ParseDecimal(s, NumberStyles.Number, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06000D37 RID: 3383 RVA: 0x00028053 File Offset: 0x00026253
		[__DynamicallyInvokable]
		public static decimal Parse(string s, NumberStyles style)
		{
			NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
			return Number.ParseDecimal(s, style, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06000D38 RID: 3384 RVA: 0x00028067 File Offset: 0x00026267
		[__DynamicallyInvokable]
		public static decimal Parse(string s, IFormatProvider provider)
		{
			return Number.ParseDecimal(s, NumberStyles.Number, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06000D39 RID: 3385 RVA: 0x00028077 File Offset: 0x00026277
		[__DynamicallyInvokable]
		public static decimal Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
			return Number.ParseDecimal(s, style, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06000D3A RID: 3386 RVA: 0x0002808C File Offset: 0x0002628C
		[__DynamicallyInvokable]
		public static bool TryParse(string s, out decimal result)
		{
			return Number.TryParseDecimal(s, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result);
		}

		// Token: 0x06000D3B RID: 3387 RVA: 0x0002809C File Offset: 0x0002629C
		[__DynamicallyInvokable]
		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out decimal result)
		{
			NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
			return Number.TryParseDecimal(s, style, NumberFormatInfo.GetInstance(provider), out result);
		}

		// Token: 0x06000D3C RID: 3388 RVA: 0x000280B2 File Offset: 0x000262B2
		[__DynamicallyInvokable]
		public static int[] GetBits(decimal d)
		{
			return new int[] { d.lo, d.mid, d.hi, d.flags };
		}

		// Token: 0x06000D3D RID: 3389 RVA: 0x000280E0 File Offset: 0x000262E0
		internal static void GetBytes(decimal d, byte[] buffer)
		{
			buffer[0] = (byte)d.lo;
			buffer[1] = (byte)(d.lo >> 8);
			buffer[2] = (byte)(d.lo >> 16);
			buffer[3] = (byte)(d.lo >> 24);
			buffer[4] = (byte)d.mid;
			buffer[5] = (byte)(d.mid >> 8);
			buffer[6] = (byte)(d.mid >> 16);
			buffer[7] = (byte)(d.mid >> 24);
			buffer[8] = (byte)d.hi;
			buffer[9] = (byte)(d.hi >> 8);
			buffer[10] = (byte)(d.hi >> 16);
			buffer[11] = (byte)(d.hi >> 24);
			buffer[12] = (byte)d.flags;
			buffer[13] = (byte)(d.flags >> 8);
			buffer[14] = (byte)(d.flags >> 16);
			buffer[15] = (byte)(d.flags >> 24);
		}

		// Token: 0x06000D3E RID: 3390 RVA: 0x000281B4 File Offset: 0x000263B4
		internal static decimal ToDecimal(byte[] buffer)
		{
			int num = (int)buffer[0] | ((int)buffer[1] << 8) | ((int)buffer[2] << 16) | ((int)buffer[3] << 24);
			int num2 = (int)buffer[4] | ((int)buffer[5] << 8) | ((int)buffer[6] << 16) | ((int)buffer[7] << 24);
			int num3 = (int)buffer[8] | ((int)buffer[9] << 8) | ((int)buffer[10] << 16) | ((int)buffer[11] << 24);
			int num4 = (int)buffer[12] | ((int)buffer[13] << 8) | ((int)buffer[14] << 16) | ((int)buffer[15] << 24);
			return new decimal(num, num2, num3, num4);
		}

		// Token: 0x06000D3F RID: 3391 RVA: 0x00028234 File Offset: 0x00026434
		private static void InternalAddUInt32RawUnchecked(ref decimal value, uint i)
		{
			uint num = (uint)value.lo;
			uint num2 = num + i;
			value.lo = (int)num2;
			if (num2 < num || num2 < i)
			{
				num = (uint)value.mid;
				num2 = num + 1U;
				value.mid = (int)num2;
				if (num2 < num || num2 < 1U)
				{
					value.hi++;
				}
			}
		}

		// Token: 0x06000D40 RID: 3392 RVA: 0x00028284 File Offset: 0x00026484
		private static uint InternalDivRemUInt32(ref decimal value, uint divisor)
		{
			uint num = 0U;
			if (value.hi != 0)
			{
				ulong num2 = (ulong)value.hi;
				value.hi = (int)((uint)(num2 / (ulong)divisor));
				num = (uint)(num2 % (ulong)divisor);
			}
			if (value.mid != 0 || num != 0U)
			{
				ulong num2 = ((ulong)num << 32) | (ulong)value.mid;
				value.mid = (int)((uint)(num2 / (ulong)divisor));
				num = (uint)(num2 % (ulong)divisor);
			}
			if (value.lo != 0 || num != 0U)
			{
				ulong num2 = ((ulong)num << 32) | (ulong)value.lo;
				value.lo = (int)((uint)(num2 / (ulong)divisor));
				num = (uint)(num2 % (ulong)divisor);
			}
			return num;
		}

		// Token: 0x06000D41 RID: 3393 RVA: 0x0002830C File Offset: 0x0002650C
		private static void InternalRoundFromZero(ref decimal d, int decimalCount)
		{
			int num = (d.flags & 16711680) >> 16;
			int num2 = num - decimalCount;
			if (num2 <= 0)
			{
				return;
			}
			uint num4;
			uint num5;
			do
			{
				int num3 = ((num2 > 9) ? 9 : num2);
				num4 = decimal.Powers10[num3];
				num5 = decimal.InternalDivRemUInt32(ref d, num4);
				num2 -= num3;
			}
			while (num2 > 0);
			if (num5 >= num4 >> 1)
			{
				decimal.InternalAddUInt32RawUnchecked(ref d, 1U);
			}
			d.flags = ((decimalCount << 16) & 16711680) | (d.flags & int.MinValue);
		}

		// Token: 0x06000D42 RID: 3394 RVA: 0x00028382 File Offset: 0x00026582
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal static decimal Max(decimal d1, decimal d2)
		{
			if (decimal.FCallCompare(ref d1, ref d2) < 0)
			{
				return d2;
			}
			return d1;
		}

		// Token: 0x06000D43 RID: 3395 RVA: 0x00028393 File Offset: 0x00026593
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal static decimal Min(decimal d1, decimal d2)
		{
			if (decimal.FCallCompare(ref d1, ref d2) >= 0)
			{
				return d2;
			}
			return d1;
		}

		// Token: 0x06000D44 RID: 3396 RVA: 0x000283A4 File Offset: 0x000265A4
		[__DynamicallyInvokable]
		public static decimal Remainder(decimal d1, decimal d2)
		{
			d2.flags = (d2.flags & int.MaxValue) | (d1.flags & int.MinValue);
			if (decimal.Abs(d1) < decimal.Abs(d2))
			{
				return d1;
			}
			d1 -= d2;
			if (d1 == 0m)
			{
				d1.flags = (d1.flags & int.MaxValue) | (d2.flags & int.MinValue);
			}
			decimal d3 = decimal.Truncate(d1 / d2);
			decimal d4 = d3 * d2;
			decimal num = d1 - d4;
			if ((d1.flags & -2147483648) != (num.flags & -2147483648))
			{
				if (-0.000000000000000000000000001m <= num && num <= 0.000000000000000000000000001m)
				{
					num.flags = (num.flags & int.MaxValue) | (d1.flags & int.MinValue);
				}
				else
				{
					num += d2;
				}
			}
			return num;
		}

		// Token: 0x06000D45 RID: 3397 RVA: 0x000284A4 File Offset: 0x000266A4
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal Multiply(decimal d1, decimal d2)
		{
			decimal.FCallMultiply(ref d1, ref d2);
			return d1;
		}

		// Token: 0x06000D46 RID: 3398
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallMultiply(ref decimal d1, ref decimal d2);

		// Token: 0x06000D47 RID: 3399
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallMultiplyOverflowed(ref decimal d1, ref decimal d2, ref bool overflowed);

		// Token: 0x06000D48 RID: 3400 RVA: 0x000284B0 File Offset: 0x000266B0
		[__DynamicallyInvokable]
		public static decimal Negate(decimal d)
		{
			return new decimal(d.lo, d.mid, d.hi, d.flags ^ int.MinValue);
		}

		// Token: 0x06000D49 RID: 3401 RVA: 0x000284D5 File Offset: 0x000266D5
		public static decimal Round(decimal d)
		{
			return decimal.Round(d, 0);
		}

		// Token: 0x06000D4A RID: 3402 RVA: 0x000284DE File Offset: 0x000266DE
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal Round(decimal d, int decimals)
		{
			decimal.FCallRound(ref d, decimals);
			return d;
		}

		// Token: 0x06000D4B RID: 3403 RVA: 0x000284E9 File Offset: 0x000266E9
		public static decimal Round(decimal d, MidpointRounding mode)
		{
			return decimal.Round(d, 0, mode);
		}

		// Token: 0x06000D4C RID: 3404 RVA: 0x000284F4 File Offset: 0x000266F4
		[SecuritySafeCritical]
		public static decimal Round(decimal d, int decimals, MidpointRounding mode)
		{
			if (decimals < 0 || decimals > 28)
			{
				throw new ArgumentOutOfRangeException("decimals", Environment.GetResourceString("ArgumentOutOfRange_DecimalRound"));
			}
			if (mode < MidpointRounding.ToEven || mode > MidpointRounding.AwayFromZero)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEnumValue", new object[] { mode, "MidpointRounding" }), "mode");
			}
			if (mode == MidpointRounding.ToEven)
			{
				decimal.FCallRound(ref d, decimals);
			}
			else
			{
				decimal.InternalRoundFromZero(ref d, decimals);
			}
			return d;
		}

		// Token: 0x06000D4D RID: 3405
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallRound(ref decimal d, int decimals);

		// Token: 0x06000D4E RID: 3406 RVA: 0x00028569 File Offset: 0x00026769
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal Subtract(decimal d1, decimal d2)
		{
			decimal.FCallAddSub(ref d1, ref d2, 128);
			return d1;
		}

		// Token: 0x06000D4F RID: 3407 RVA: 0x0002857C File Offset: 0x0002677C
		[__DynamicallyInvokable]
		public static byte ToByte(decimal value)
		{
			uint num;
			try
			{
				num = decimal.ToUInt32(value);
			}
			catch (OverflowException innerException)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"), innerException);
			}
			if (num < 0U || num > 255U)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
			}
			return (byte)num;
		}

		// Token: 0x06000D50 RID: 3408 RVA: 0x000285D4 File Offset: 0x000267D4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(decimal value)
		{
			int num;
			try
			{
				num = decimal.ToInt32(value);
			}
			catch (OverflowException innerException)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"), innerException);
			}
			if (num < -128 || num > 127)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
			}
			return (sbyte)num;
		}

		// Token: 0x06000D51 RID: 3409 RVA: 0x00028628 File Offset: 0x00026828
		[__DynamicallyInvokable]
		public static short ToInt16(decimal value)
		{
			int num;
			try
			{
				num = decimal.ToInt32(value);
			}
			catch (OverflowException innerException)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int16"), innerException);
			}
			if (num < -32768 || num > 32767)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
			}
			return (short)num;
		}

		// Token: 0x06000D52 RID: 3410 RVA: 0x00028684 File Offset: 0x00026884
		[SecuritySafeCritical]
		internal static Currency ToCurrency(decimal d)
		{
			Currency result = default(Currency);
			decimal.FCallToCurrency(ref result, d);
			return result;
		}

		// Token: 0x06000D53 RID: 3411
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallToCurrency(ref Currency result, decimal d);

		// Token: 0x06000D54 RID: 3412
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double ToDouble(decimal d);

		// Token: 0x06000D55 RID: 3413
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int FCallToInt32(decimal d);

		// Token: 0x06000D56 RID: 3414 RVA: 0x000286A4 File Offset: 0x000268A4
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static int ToInt32(decimal d)
		{
			if ((d.flags & 16711680) != 0)
			{
				decimal.FCallTruncate(ref d);
			}
			if (d.hi == 0 && d.mid == 0)
			{
				int num = d.lo;
				if (d.flags >= 0)
				{
					if (num >= 0)
					{
						return num;
					}
				}
				else
				{
					num = -num;
					if (num <= 0)
					{
						return num;
					}
				}
			}
			throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
		}

		// Token: 0x06000D57 RID: 3415 RVA: 0x00028704 File Offset: 0x00026904
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static long ToInt64(decimal d)
		{
			if ((d.flags & 16711680) != 0)
			{
				decimal.FCallTruncate(ref d);
			}
			if (d.hi == 0)
			{
				long num = ((long)d.lo & (long)((ulong)(-1))) | ((long)d.mid << 32);
				if (d.flags >= 0)
				{
					if (num >= 0L)
					{
						return num;
					}
				}
				else
				{
					num = -num;
					if (num <= 0L)
					{
						return num;
					}
				}
			}
			throw new OverflowException(Environment.GetResourceString("Overflow_Int64"));
		}

		// Token: 0x06000D58 RID: 3416 RVA: 0x00028770 File Offset: 0x00026970
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(decimal value)
		{
			uint num;
			try
			{
				num = decimal.ToUInt32(value);
			}
			catch (OverflowException innerException)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"), innerException);
			}
			if (num < 0U || num > 65535U)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
			}
			return (ushort)num;
		}

		// Token: 0x06000D59 RID: 3417 RVA: 0x000287C8 File Offset: 0x000269C8
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(decimal d)
		{
			if ((d.flags & 16711680) != 0)
			{
				decimal.FCallTruncate(ref d);
			}
			if (d.hi == 0 && d.mid == 0)
			{
				uint num = (uint)d.lo;
				if (d.flags >= 0 || num == 0U)
				{
					return num;
				}
			}
			throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
		}

		// Token: 0x06000D5A RID: 3418 RVA: 0x00028820 File Offset: 0x00026A20
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(decimal d)
		{
			if ((d.flags & 16711680) != 0)
			{
				decimal.FCallTruncate(ref d);
			}
			if (d.hi == 0)
			{
				ulong num = (ulong)d.lo | ((ulong)d.mid << 32);
				if (d.flags >= 0 || num == 0UL)
				{
					return num;
				}
			}
			throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
		}

		// Token: 0x06000D5B RID: 3419
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern float ToSingle(decimal d);

		// Token: 0x06000D5C RID: 3420 RVA: 0x0002887A File Offset: 0x00026A7A
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal Truncate(decimal d)
		{
			decimal.FCallTruncate(ref d);
			return d;
		}

		// Token: 0x06000D5D RID: 3421
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void FCallTruncate(ref decimal d);

		// Token: 0x06000D5E RID: 3422 RVA: 0x00028884 File Offset: 0x00026A84
		[__DynamicallyInvokable]
		public static implicit operator decimal(byte value)
		{
			return new decimal((int)value);
		}

		// Token: 0x06000D5F RID: 3423 RVA: 0x0002888C File Offset: 0x00026A8C
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static implicit operator decimal(sbyte value)
		{
			return new decimal((int)value);
		}

		// Token: 0x06000D60 RID: 3424 RVA: 0x00028894 File Offset: 0x00026A94
		[__DynamicallyInvokable]
		public static implicit operator decimal(short value)
		{
			return new decimal((int)value);
		}

		// Token: 0x06000D61 RID: 3425 RVA: 0x0002889C File Offset: 0x00026A9C
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static implicit operator decimal(ushort value)
		{
			return new decimal((int)value);
		}

		// Token: 0x06000D62 RID: 3426 RVA: 0x000288A4 File Offset: 0x00026AA4
		[__DynamicallyInvokable]
		public static implicit operator decimal(char value)
		{
			return new decimal((int)value);
		}

		// Token: 0x06000D63 RID: 3427 RVA: 0x000288AC File Offset: 0x00026AAC
		[__DynamicallyInvokable]
		public static implicit operator decimal(int value)
		{
			return new decimal(value);
		}

		// Token: 0x06000D64 RID: 3428 RVA: 0x000288B4 File Offset: 0x00026AB4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static implicit operator decimal(uint value)
		{
			return new decimal(value);
		}

		// Token: 0x06000D65 RID: 3429 RVA: 0x000288BC File Offset: 0x00026ABC
		[__DynamicallyInvokable]
		public static implicit operator decimal(long value)
		{
			return new decimal(value);
		}

		// Token: 0x06000D66 RID: 3430 RVA: 0x000288C4 File Offset: 0x00026AC4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static implicit operator decimal(ulong value)
		{
			return new decimal(value);
		}

		// Token: 0x06000D67 RID: 3431 RVA: 0x000288CC File Offset: 0x00026ACC
		[__DynamicallyInvokable]
		public static explicit operator decimal(float value)
		{
			return new decimal(value);
		}

		// Token: 0x06000D68 RID: 3432 RVA: 0x000288D4 File Offset: 0x00026AD4
		[__DynamicallyInvokable]
		public static explicit operator decimal(double value)
		{
			return new decimal(value);
		}

		// Token: 0x06000D69 RID: 3433 RVA: 0x000288DC File Offset: 0x00026ADC
		[__DynamicallyInvokable]
		public static explicit operator byte(decimal value)
		{
			return decimal.ToByte(value);
		}

		// Token: 0x06000D6A RID: 3434 RVA: 0x000288E4 File Offset: 0x00026AE4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static explicit operator sbyte(decimal value)
		{
			return decimal.ToSByte(value);
		}

		// Token: 0x06000D6B RID: 3435 RVA: 0x000288EC File Offset: 0x00026AEC
		[__DynamicallyInvokable]
		public static explicit operator char(decimal value)
		{
			ushort result;
			try
			{
				result = decimal.ToUInt16(value);
			}
			catch (OverflowException innerException)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Char"), innerException);
			}
			return (char)result;
		}

		// Token: 0x06000D6C RID: 3436 RVA: 0x00028928 File Offset: 0x00026B28
		[__DynamicallyInvokable]
		public static explicit operator short(decimal value)
		{
			return decimal.ToInt16(value);
		}

		// Token: 0x06000D6D RID: 3437 RVA: 0x00028930 File Offset: 0x00026B30
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static explicit operator ushort(decimal value)
		{
			return decimal.ToUInt16(value);
		}

		// Token: 0x06000D6E RID: 3438 RVA: 0x00028938 File Offset: 0x00026B38
		[__DynamicallyInvokable]
		public static explicit operator int(decimal value)
		{
			return decimal.ToInt32(value);
		}

		// Token: 0x06000D6F RID: 3439 RVA: 0x00028940 File Offset: 0x00026B40
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static explicit operator uint(decimal value)
		{
			return decimal.ToUInt32(value);
		}

		// Token: 0x06000D70 RID: 3440 RVA: 0x00028948 File Offset: 0x00026B48
		[__DynamicallyInvokable]
		public static explicit operator long(decimal value)
		{
			return decimal.ToInt64(value);
		}

		// Token: 0x06000D71 RID: 3441 RVA: 0x00028950 File Offset: 0x00026B50
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static explicit operator ulong(decimal value)
		{
			return decimal.ToUInt64(value);
		}

		// Token: 0x06000D72 RID: 3442 RVA: 0x00028958 File Offset: 0x00026B58
		[__DynamicallyInvokable]
		public static explicit operator float(decimal value)
		{
			return decimal.ToSingle(value);
		}

		// Token: 0x06000D73 RID: 3443 RVA: 0x00028960 File Offset: 0x00026B60
		[__DynamicallyInvokable]
		public static explicit operator double(decimal value)
		{
			return decimal.ToDouble(value);
		}

		// Token: 0x06000D74 RID: 3444 RVA: 0x00028968 File Offset: 0x00026B68
		[__DynamicallyInvokable]
		public static decimal operator +(decimal d)
		{
			return d;
		}

		// Token: 0x06000D75 RID: 3445 RVA: 0x0002896B File Offset: 0x00026B6B
		[__DynamicallyInvokable]
		public static decimal operator -(decimal d)
		{
			return decimal.Negate(d);
		}

		// Token: 0x06000D76 RID: 3446 RVA: 0x00028973 File Offset: 0x00026B73
		[__DynamicallyInvokable]
		public static decimal operator ++(decimal d)
		{
			return decimal.Add(d, 1m);
		}

		// Token: 0x06000D77 RID: 3447 RVA: 0x00028980 File Offset: 0x00026B80
		[__DynamicallyInvokable]
		public static decimal operator --(decimal d)
		{
			return decimal.Subtract(d, 1m);
		}

		// Token: 0x06000D78 RID: 3448 RVA: 0x0002898D File Offset: 0x00026B8D
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal operator +(decimal d1, decimal d2)
		{
			decimal.FCallAddSub(ref d1, ref d2, 0);
			return d1;
		}

		// Token: 0x06000D79 RID: 3449 RVA: 0x0002899A File Offset: 0x00026B9A
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal operator -(decimal d1, decimal d2)
		{
			decimal.FCallAddSub(ref d1, ref d2, 128);
			return d1;
		}

		// Token: 0x06000D7A RID: 3450 RVA: 0x000289AB File Offset: 0x00026BAB
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal operator *(decimal d1, decimal d2)
		{
			decimal.FCallMultiply(ref d1, ref d2);
			return d1;
		}

		// Token: 0x06000D7B RID: 3451 RVA: 0x000289B7 File Offset: 0x00026BB7
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static decimal operator /(decimal d1, decimal d2)
		{
			decimal.FCallDivide(ref d1, ref d2);
			return d1;
		}

		// Token: 0x06000D7C RID: 3452 RVA: 0x000289C3 File Offset: 0x00026BC3
		[__DynamicallyInvokable]
		public static decimal operator %(decimal d1, decimal d2)
		{
			return decimal.Remainder(d1, d2);
		}

		// Token: 0x06000D7D RID: 3453 RVA: 0x000289CC File Offset: 0x00026BCC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static bool operator ==(decimal d1, decimal d2)
		{
			return decimal.FCallCompare(ref d1, ref d2) == 0;
		}

		// Token: 0x06000D7E RID: 3454 RVA: 0x000289DA File Offset: 0x00026BDA
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static bool operator !=(decimal d1, decimal d2)
		{
			return decimal.FCallCompare(ref d1, ref d2) != 0;
		}

		// Token: 0x06000D7F RID: 3455 RVA: 0x000289E8 File Offset: 0x00026BE8
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static bool operator <(decimal d1, decimal d2)
		{
			return decimal.FCallCompare(ref d1, ref d2) < 0;
		}

		// Token: 0x06000D80 RID: 3456 RVA: 0x000289F6 File Offset: 0x00026BF6
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static bool operator <=(decimal d1, decimal d2)
		{
			return decimal.FCallCompare(ref d1, ref d2) <= 0;
		}

		// Token: 0x06000D81 RID: 3457 RVA: 0x00028A07 File Offset: 0x00026C07
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static bool operator >(decimal d1, decimal d2)
		{
			return decimal.FCallCompare(ref d1, ref d2) > 0;
		}

		// Token: 0x06000D82 RID: 3458 RVA: 0x00028A15 File Offset: 0x00026C15
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static bool operator >=(decimal d1, decimal d2)
		{
			return decimal.FCallCompare(ref d1, ref d2) >= 0;
		}

		// Token: 0x06000D83 RID: 3459 RVA: 0x00028A26 File Offset: 0x00026C26
		public TypeCode GetTypeCode()
		{
			return TypeCode.Decimal;
		}

		// Token: 0x06000D84 RID: 3460 RVA: 0x00028A2A File Offset: 0x00026C2A
		[__DynamicallyInvokable]
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return Convert.ToBoolean(this);
		}

		// Token: 0x06000D85 RID: 3461 RVA: 0x00028A37 File Offset: 0x00026C37
		[__DynamicallyInvokable]
		char IConvertible.ToChar(IFormatProvider provider)
		{
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[] { "Decimal", "Char" }));
		}

		// Token: 0x06000D86 RID: 3462 RVA: 0x00028A5E File Offset: 0x00026C5E
		[__DynamicallyInvokable]
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return Convert.ToSByte(this);
		}

		// Token: 0x06000D87 RID: 3463 RVA: 0x00028A6B File Offset: 0x00026C6B
		[__DynamicallyInvokable]
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return Convert.ToByte(this);
		}

		// Token: 0x06000D88 RID: 3464 RVA: 0x00028A78 File Offset: 0x00026C78
		[__DynamicallyInvokable]
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return Convert.ToInt16(this);
		}

		// Token: 0x06000D89 RID: 3465 RVA: 0x00028A85 File Offset: 0x00026C85
		[__DynamicallyInvokable]
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return Convert.ToUInt16(this);
		}

		// Token: 0x06000D8A RID: 3466 RVA: 0x00028A92 File Offset: 0x00026C92
		[__DynamicallyInvokable]
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return Convert.ToInt32(this);
		}

		// Token: 0x06000D8B RID: 3467 RVA: 0x00028A9F File Offset: 0x00026C9F
		[__DynamicallyInvokable]
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32(this);
		}

		// Token: 0x06000D8C RID: 3468 RVA: 0x00028AAC File Offset: 0x00026CAC
		[__DynamicallyInvokable]
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return Convert.ToInt64(this);
		}

		// Token: 0x06000D8D RID: 3469 RVA: 0x00028AB9 File Offset: 0x00026CB9
		[__DynamicallyInvokable]
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return Convert.ToUInt64(this);
		}

		// Token: 0x06000D8E RID: 3470 RVA: 0x00028AC6 File Offset: 0x00026CC6
		[__DynamicallyInvokable]
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return Convert.ToSingle(this);
		}

		// Token: 0x06000D8F RID: 3471 RVA: 0x00028AD3 File Offset: 0x00026CD3
		[__DynamicallyInvokable]
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(this);
		}

		// Token: 0x06000D90 RID: 3472 RVA: 0x00028AE0 File Offset: 0x00026CE0
		[__DynamicallyInvokable]
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return this;
		}

		// Token: 0x06000D91 RID: 3473 RVA: 0x00028AE8 File Offset: 0x00026CE8
		[__DynamicallyInvokable]
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[] { "Decimal", "DateTime" }));
		}

		// Token: 0x06000D92 RID: 3474 RVA: 0x00028B0F File Offset: 0x00026D0F
		[__DynamicallyInvokable]
		object IConvertible.ToType(Type type, IFormatProvider provider)
		{
			return Convert.DefaultToType(this, type, provider);
		}

		// Token: 0x0400054B RID: 1355
		private const int SignMask = -2147483648;

		// Token: 0x0400054C RID: 1356
		private const byte DECIMAL_NEG = 128;

		// Token: 0x0400054D RID: 1357
		private const byte DECIMAL_ADD = 0;

		// Token: 0x0400054E RID: 1358
		private const int ScaleMask = 16711680;

		// Token: 0x0400054F RID: 1359
		private const int ScaleShift = 16;

		// Token: 0x04000550 RID: 1360
		private const int MaxInt32Scale = 9;

		// Token: 0x04000551 RID: 1361
		private static uint[] Powers10 = new uint[] { 1U, 10U, 100U, 1000U, 10000U, 100000U, 1000000U, 10000000U, 100000000U, 1000000000U };

		// Token: 0x04000552 RID: 1362
		[__DynamicallyInvokable]
		public const decimal Zero = 0m;

		// Token: 0x04000553 RID: 1363
		[__DynamicallyInvokable]
		public const decimal One = 1m;

		// Token: 0x04000554 RID: 1364
		[__DynamicallyInvokable]
		public const decimal MinusOne = -1m;

		// Token: 0x04000555 RID: 1365
		[__DynamicallyInvokable]
		public const decimal MaxValue = 79228162514264337593543950335m;

		// Token: 0x04000556 RID: 1366
		[__DynamicallyInvokable]
		public const decimal MinValue = -79228162514264337593543950335m;

		// Token: 0x04000557 RID: 1367
		private const decimal NearNegativeZero = -0.000000000000000000000000001m;

		// Token: 0x04000558 RID: 1368
		private const decimal NearPositiveZero = 0.000000000000000000000000001m;

		// Token: 0x04000559 RID: 1369
		private int flags;

		// Token: 0x0400055A RID: 1370
		private int hi;

		// Token: 0x0400055B RID: 1371
		private int lo;

		// Token: 0x0400055C RID: 1372
		private int mid;
	}
}
