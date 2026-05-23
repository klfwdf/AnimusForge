using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace System
{
	// Token: 0x020000FA RID: 250
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct Int16 : IComparable, IFormattable, IConvertible, IComparable<short>, IEquatable<short>
	{
		// Token: 0x06000F29 RID: 3881 RVA: 0x0002F242 File Offset: 0x0002D442
		public int CompareTo(object value)
		{
			if (value == null)
			{
				return 1;
			}
			if (value is short)
			{
				return (int)(this - (short)value);
			}
			throw new ArgumentException(Environment.GetResourceString("Arg_MustBeInt16"));
		}

		// Token: 0x06000F2A RID: 3882 RVA: 0x0002F26A File Offset: 0x0002D46A
		[__DynamicallyInvokable]
		public int CompareTo(short value)
		{
			return (int)(this - value);
		}

		// Token: 0x06000F2B RID: 3883 RVA: 0x0002F270 File Offset: 0x0002D470
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is short && this == (short)obj;
		}

		// Token: 0x06000F2C RID: 3884 RVA: 0x0002F286 File Offset: 0x0002D486
		[NonVersionable]
		[__DynamicallyInvokable]
		public bool Equals(short obj)
		{
			return this == obj;
		}

		// Token: 0x06000F2D RID: 3885 RVA: 0x0002F28D File Offset: 0x0002D48D
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return (int)((ushort)this) | ((int)this << 16);
		}

		// Token: 0x06000F2E RID: 3886 RVA: 0x0002F298 File Offset: 0x0002D498
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return Number.FormatInt32((int)this, null, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06000F2F RID: 3887 RVA: 0x0002F2A7 File Offset: 0x0002D4A7
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(IFormatProvider provider)
		{
			return Number.FormatInt32((int)this, null, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06000F30 RID: 3888 RVA: 0x0002F2B7 File Offset: 0x0002D4B7
		[__DynamicallyInvokable]
		public string ToString(string format)
		{
			return this.ToString(format, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06000F31 RID: 3889 RVA: 0x0002F2C5 File Offset: 0x0002D4C5
		[__DynamicallyInvokable]
		public string ToString(string format, IFormatProvider provider)
		{
			return this.ToString(format, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06000F32 RID: 3890 RVA: 0x0002F2D4 File Offset: 0x0002D4D4
		[SecuritySafeCritical]
		private string ToString(string format, NumberFormatInfo info)
		{
			if (this < 0 && format != null && format.Length > 0 && (format[0] == 'X' || format[0] == 'x'))
			{
				uint value = (uint)this & 65535U;
				return Number.FormatUInt32(value, format, info);
			}
			return Number.FormatInt32((int)this, format, info);
		}

		// Token: 0x06000F33 RID: 3891 RVA: 0x0002F323 File Offset: 0x0002D523
		[__DynamicallyInvokable]
		public static short Parse(string s)
		{
			return short.Parse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06000F34 RID: 3892 RVA: 0x0002F331 File Offset: 0x0002D531
		[__DynamicallyInvokable]
		public static short Parse(string s, NumberStyles style)
		{
			NumberFormatInfo.ValidateParseStyleInteger(style);
			return short.Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06000F35 RID: 3893 RVA: 0x0002F345 File Offset: 0x0002D545
		[__DynamicallyInvokable]
		public static short Parse(string s, IFormatProvider provider)
		{
			return short.Parse(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06000F36 RID: 3894 RVA: 0x0002F354 File Offset: 0x0002D554
		[__DynamicallyInvokable]
		public static short Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			NumberFormatInfo.ValidateParseStyleInteger(style);
			return short.Parse(s, style, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06000F37 RID: 3895 RVA: 0x0002F36C File Offset: 0x0002D56C
		private static short Parse(string s, NumberStyles style, NumberFormatInfo info)
		{
			int num = 0;
			try
			{
				num = Number.ParseInt32(s, style, info);
			}
			catch (OverflowException innerException)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int16"), innerException);
			}
			if ((style & NumberStyles.AllowHexSpecifier) != NumberStyles.None)
			{
				if (num < 0 || num > 65535)
				{
					throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
				}
				return (short)num;
			}
			else
			{
				if (num < -32768 || num > 32767)
				{
					throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
				}
				return (short)num;
			}
		}

		// Token: 0x06000F38 RID: 3896 RVA: 0x0002F3F4 File Offset: 0x0002D5F4
		[__DynamicallyInvokable]
		public static bool TryParse(string s, out short result)
		{
			return short.TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		// Token: 0x06000F39 RID: 3897 RVA: 0x0002F403 File Offset: 0x0002D603
		[__DynamicallyInvokable]
		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out short result)
		{
			NumberFormatInfo.ValidateParseStyleInteger(style);
			return short.TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
		}

		// Token: 0x06000F3A RID: 3898 RVA: 0x0002F41C File Offset: 0x0002D61C
		private static bool TryParse(string s, NumberStyles style, NumberFormatInfo info, out short result)
		{
			result = 0;
			int num;
			if (!Number.TryParseInt32(s, style, info, out num))
			{
				return false;
			}
			if ((style & NumberStyles.AllowHexSpecifier) != NumberStyles.None)
			{
				if (num < 0 || num > 65535)
				{
					return false;
				}
				result = (short)num;
				return true;
			}
			else
			{
				if (num < -32768 || num > 32767)
				{
					return false;
				}
				result = (short)num;
				return true;
			}
		}

		// Token: 0x06000F3B RID: 3899 RVA: 0x0002F46E File Offset: 0x0002D66E
		public TypeCode GetTypeCode()
		{
			return TypeCode.Int16;
		}

		// Token: 0x06000F3C RID: 3900 RVA: 0x0002F471 File Offset: 0x0002D671
		[__DynamicallyInvokable]
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return Convert.ToBoolean(this);
		}

		// Token: 0x06000F3D RID: 3901 RVA: 0x0002F47A File Offset: 0x0002D67A
		[__DynamicallyInvokable]
		char IConvertible.ToChar(IFormatProvider provider)
		{
			return Convert.ToChar(this);
		}

		// Token: 0x06000F3E RID: 3902 RVA: 0x0002F483 File Offset: 0x0002D683
		[__DynamicallyInvokable]
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return Convert.ToSByte(this);
		}

		// Token: 0x06000F3F RID: 3903 RVA: 0x0002F48C File Offset: 0x0002D68C
		[__DynamicallyInvokable]
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return Convert.ToByte(this);
		}

		// Token: 0x06000F40 RID: 3904 RVA: 0x0002F495 File Offset: 0x0002D695
		[__DynamicallyInvokable]
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return this;
		}

		// Token: 0x06000F41 RID: 3905 RVA: 0x0002F499 File Offset: 0x0002D699
		[__DynamicallyInvokable]
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return Convert.ToUInt16(this);
		}

		// Token: 0x06000F42 RID: 3906 RVA: 0x0002F4A2 File Offset: 0x0002D6A2
		[__DynamicallyInvokable]
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return Convert.ToInt32(this);
		}

		// Token: 0x06000F43 RID: 3907 RVA: 0x0002F4AB File Offset: 0x0002D6AB
		[__DynamicallyInvokable]
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32(this);
		}

		// Token: 0x06000F44 RID: 3908 RVA: 0x0002F4B4 File Offset: 0x0002D6B4
		[__DynamicallyInvokable]
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return Convert.ToInt64(this);
		}

		// Token: 0x06000F45 RID: 3909 RVA: 0x0002F4BD File Offset: 0x0002D6BD
		[__DynamicallyInvokable]
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return Convert.ToUInt64(this);
		}

		// Token: 0x06000F46 RID: 3910 RVA: 0x0002F4C6 File Offset: 0x0002D6C6
		[__DynamicallyInvokable]
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return Convert.ToSingle(this);
		}

		// Token: 0x06000F47 RID: 3911 RVA: 0x0002F4CF File Offset: 0x0002D6CF
		[__DynamicallyInvokable]
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(this);
		}

		// Token: 0x06000F48 RID: 3912 RVA: 0x0002F4D8 File Offset: 0x0002D6D8
		[__DynamicallyInvokable]
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return Convert.ToDecimal(this);
		}

		// Token: 0x06000F49 RID: 3913 RVA: 0x0002F4E1 File Offset: 0x0002D6E1
		[__DynamicallyInvokable]
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[] { "Int16", "DateTime" }));
		}

		// Token: 0x06000F4A RID: 3914 RVA: 0x0002F508 File Offset: 0x0002D708
		[__DynamicallyInvokable]
		object IConvertible.ToType(Type type, IFormatProvider provider)
		{
			return Convert.DefaultToType(this, type, provider);
		}

		// Token: 0x0400059F RID: 1439
		internal short m_value;

		// Token: 0x040005A0 RID: 1440
		[__DynamicallyInvokable]
		public const short MaxValue = 32767;

		// Token: 0x040005A1 RID: 1441
		[__DynamicallyInvokable]
		public const short MinValue = -32768;
	}
}
