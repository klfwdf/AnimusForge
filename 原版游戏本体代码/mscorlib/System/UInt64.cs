using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace System
{
	// Token: 0x02000152 RID: 338
	[CLSCompliant(false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct UInt64 : IComparable, IFormattable, IConvertible, IComparable<ulong>, IEquatable<ulong>
	{
		// Token: 0x06001522 RID: 5410 RVA: 0x0003E288 File Offset: 0x0003C488
		public int CompareTo(object value)
		{
			if (value == null)
			{
				return 1;
			}
			if (!(value is ulong))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeUInt64"));
			}
			ulong num = (ulong)value;
			if (this < num)
			{
				return -1;
			}
			if (this > num)
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x06001523 RID: 5411 RVA: 0x0003E2C8 File Offset: 0x0003C4C8
		[__DynamicallyInvokable]
		public int CompareTo(ulong value)
		{
			if (this < value)
			{
				return -1;
			}
			if (this > value)
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x06001524 RID: 5412 RVA: 0x0003E2D9 File Offset: 0x0003C4D9
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is ulong && this == (ulong)obj;
		}

		// Token: 0x06001525 RID: 5413 RVA: 0x0003E2EF File Offset: 0x0003C4EF
		[NonVersionable]
		[__DynamicallyInvokable]
		public bool Equals(ulong obj)
		{
			return this == obj;
		}

		// Token: 0x06001526 RID: 5414 RVA: 0x0003E2F6 File Offset: 0x0003C4F6
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return (int)this ^ (int)(this >> 32);
		}

		// Token: 0x06001527 RID: 5415 RVA: 0x0003E302 File Offset: 0x0003C502
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return Number.FormatUInt64(this, null, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06001528 RID: 5416 RVA: 0x0003E311 File Offset: 0x0003C511
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(IFormatProvider provider)
		{
			return Number.FormatUInt64(this, null, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06001529 RID: 5417 RVA: 0x0003E321 File Offset: 0x0003C521
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(string format)
		{
			return Number.FormatUInt64(this, format, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x0600152A RID: 5418 RVA: 0x0003E330 File Offset: 0x0003C530
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(string format, IFormatProvider provider)
		{
			return Number.FormatUInt64(this, format, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x0600152B RID: 5419 RVA: 0x0003E340 File Offset: 0x0003C540
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong Parse(string s)
		{
			return Number.ParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x0600152C RID: 5420 RVA: 0x0003E34E File Offset: 0x0003C54E
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong Parse(string s, NumberStyles style)
		{
			NumberFormatInfo.ValidateParseStyleInteger(style);
			return Number.ParseUInt64(s, style, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x0600152D RID: 5421 RVA: 0x0003E362 File Offset: 0x0003C562
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong Parse(string s, IFormatProvider provider)
		{
			return Number.ParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x0600152E RID: 5422 RVA: 0x0003E371 File Offset: 0x0003C571
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			NumberFormatInfo.ValidateParseStyleInteger(style);
			return Number.ParseUInt64(s, style, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x0600152F RID: 5423 RVA: 0x0003E386 File Offset: 0x0003C586
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static bool TryParse(string s, out ulong result)
		{
			return Number.TryParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		// Token: 0x06001530 RID: 5424 RVA: 0x0003E395 File Offset: 0x0003C595
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out ulong result)
		{
			NumberFormatInfo.ValidateParseStyleInteger(style);
			return Number.TryParseUInt64(s, style, NumberFormatInfo.GetInstance(provider), out result);
		}

		// Token: 0x06001531 RID: 5425 RVA: 0x0003E3AB File Offset: 0x0003C5AB
		public TypeCode GetTypeCode()
		{
			return TypeCode.UInt64;
		}

		// Token: 0x06001532 RID: 5426 RVA: 0x0003E3AF File Offset: 0x0003C5AF
		[__DynamicallyInvokable]
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return Convert.ToBoolean(this);
		}

		// Token: 0x06001533 RID: 5427 RVA: 0x0003E3B8 File Offset: 0x0003C5B8
		[__DynamicallyInvokable]
		char IConvertible.ToChar(IFormatProvider provider)
		{
			return Convert.ToChar(this);
		}

		// Token: 0x06001534 RID: 5428 RVA: 0x0003E3C1 File Offset: 0x0003C5C1
		[__DynamicallyInvokable]
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return Convert.ToSByte(this);
		}

		// Token: 0x06001535 RID: 5429 RVA: 0x0003E3CA File Offset: 0x0003C5CA
		[__DynamicallyInvokable]
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return Convert.ToByte(this);
		}

		// Token: 0x06001536 RID: 5430 RVA: 0x0003E3D3 File Offset: 0x0003C5D3
		[__DynamicallyInvokable]
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return Convert.ToInt16(this);
		}

		// Token: 0x06001537 RID: 5431 RVA: 0x0003E3DC File Offset: 0x0003C5DC
		[__DynamicallyInvokable]
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return Convert.ToUInt16(this);
		}

		// Token: 0x06001538 RID: 5432 RVA: 0x0003E3E5 File Offset: 0x0003C5E5
		[__DynamicallyInvokable]
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return Convert.ToInt32(this);
		}

		// Token: 0x06001539 RID: 5433 RVA: 0x0003E3EE File Offset: 0x0003C5EE
		[__DynamicallyInvokable]
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32(this);
		}

		// Token: 0x0600153A RID: 5434 RVA: 0x0003E3F7 File Offset: 0x0003C5F7
		[__DynamicallyInvokable]
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return Convert.ToInt64(this);
		}

		// Token: 0x0600153B RID: 5435 RVA: 0x0003E400 File Offset: 0x0003C600
		[__DynamicallyInvokable]
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return this;
		}

		// Token: 0x0600153C RID: 5436 RVA: 0x0003E404 File Offset: 0x0003C604
		[__DynamicallyInvokable]
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return Convert.ToSingle(this);
		}

		// Token: 0x0600153D RID: 5437 RVA: 0x0003E40D File Offset: 0x0003C60D
		[__DynamicallyInvokable]
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(this);
		}

		// Token: 0x0600153E RID: 5438 RVA: 0x0003E416 File Offset: 0x0003C616
		[__DynamicallyInvokable]
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return Convert.ToDecimal(this);
		}

		// Token: 0x0600153F RID: 5439 RVA: 0x0003E41F File Offset: 0x0003C61F
		[__DynamicallyInvokable]
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[] { "UInt64", "DateTime" }));
		}

		// Token: 0x06001540 RID: 5440 RVA: 0x0003E446 File Offset: 0x0003C646
		[__DynamicallyInvokable]
		object IConvertible.ToType(Type type, IFormatProvider provider)
		{
			return Convert.DefaultToType(this, type, provider);
		}

		// Token: 0x040006F7 RID: 1783
		private ulong m_value;

		// Token: 0x040006F8 RID: 1784
		[__DynamicallyInvokable]
		public const ulong MaxValue = 18446744073709551615UL;

		// Token: 0x040006F9 RID: 1785
		[__DynamicallyInvokable]
		public const ulong MinValue = 0UL;
	}
}
