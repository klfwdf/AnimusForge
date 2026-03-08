using System;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace System
{
	// Token: 0x0200013F RID: 319
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct Single : IComparable, IFormattable, IConvertible, IComparable<float>, IEquatable<float>
	{
		// Token: 0x06001315 RID: 4885 RVA: 0x00038464 File Offset: 0x00036664
		[SecuritySafeCritical]
		[NonVersionable]
		[__DynamicallyInvokable]
		public unsafe static bool IsInfinity(float f)
		{
			return (*(int*)(&f) & int.MaxValue) == 2139095040;
		}

		// Token: 0x06001316 RID: 4886 RVA: 0x00038477 File Offset: 0x00036677
		[SecuritySafeCritical]
		[NonVersionable]
		[__DynamicallyInvokable]
		public unsafe static bool IsPositiveInfinity(float f)
		{
			return *(int*)(&f) == 2139095040;
		}

		// Token: 0x06001317 RID: 4887 RVA: 0x00038484 File Offset: 0x00036684
		[SecuritySafeCritical]
		[NonVersionable]
		[__DynamicallyInvokable]
		public unsafe static bool IsNegativeInfinity(float f)
		{
			return *(int*)(&f) == -8388608;
		}

		// Token: 0x06001318 RID: 4888 RVA: 0x00038491 File Offset: 0x00036691
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SecuritySafeCritical]
		[NonVersionable]
		[__DynamicallyInvokable]
		public unsafe static bool IsNaN(float f)
		{
			return (*(int*)(&f) & int.MaxValue) > 2139095040;
		}

		// Token: 0x06001319 RID: 4889 RVA: 0x000384A4 File Offset: 0x000366A4
		public int CompareTo(object value)
		{
			if (value == null)
			{
				return 1;
			}
			if (!(value is float))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeSingle"));
			}
			float num = (float)value;
			if (this < num)
			{
				return -1;
			}
			if (this > num)
			{
				return 1;
			}
			if (this == num)
			{
				return 0;
			}
			if (!float.IsNaN(this))
			{
				return 1;
			}
			if (!float.IsNaN(num))
			{
				return -1;
			}
			return 0;
		}

		// Token: 0x0600131A RID: 4890 RVA: 0x00038500 File Offset: 0x00036700
		[__DynamicallyInvokable]
		public int CompareTo(float value)
		{
			if (this < value)
			{
				return -1;
			}
			if (this > value)
			{
				return 1;
			}
			if (this == value)
			{
				return 0;
			}
			if (!float.IsNaN(this))
			{
				return 1;
			}
			if (!float.IsNaN(value))
			{
				return -1;
			}
			return 0;
		}

		// Token: 0x0600131B RID: 4891 RVA: 0x0003852D File Offset: 0x0003672D
		[NonVersionable]
		[__DynamicallyInvokable]
		public static bool operator ==(float left, float right)
		{
			return left == right;
		}

		// Token: 0x0600131C RID: 4892 RVA: 0x00038533 File Offset: 0x00036733
		[NonVersionable]
		[__DynamicallyInvokable]
		public static bool operator !=(float left, float right)
		{
			return left != right;
		}

		// Token: 0x0600131D RID: 4893 RVA: 0x0003853C File Offset: 0x0003673C
		[NonVersionable]
		[__DynamicallyInvokable]
		public static bool operator <(float left, float right)
		{
			return left < right;
		}

		// Token: 0x0600131E RID: 4894 RVA: 0x00038542 File Offset: 0x00036742
		[NonVersionable]
		[__DynamicallyInvokable]
		public static bool operator >(float left, float right)
		{
			return left > right;
		}

		// Token: 0x0600131F RID: 4895 RVA: 0x00038548 File Offset: 0x00036748
		[NonVersionable]
		[__DynamicallyInvokable]
		public static bool operator <=(float left, float right)
		{
			return left <= right;
		}

		// Token: 0x06001320 RID: 4896 RVA: 0x00038551 File Offset: 0x00036751
		[NonVersionable]
		[__DynamicallyInvokable]
		public static bool operator >=(float left, float right)
		{
			return left >= right;
		}

		// Token: 0x06001321 RID: 4897 RVA: 0x0003855C File Offset: 0x0003675C
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			if (!(obj is float))
			{
				return false;
			}
			float num = (float)obj;
			return num == this || (float.IsNaN(num) && float.IsNaN(this));
		}

		// Token: 0x06001322 RID: 4898 RVA: 0x00038592 File Offset: 0x00036792
		[__DynamicallyInvokable]
		public bool Equals(float obj)
		{
			return obj == this || (float.IsNaN(obj) && float.IsNaN(this));
		}

		// Token: 0x06001323 RID: 4899 RVA: 0x000385AC File Offset: 0x000367AC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe override int GetHashCode()
		{
			float num = this;
			if (num == 0f)
			{
				return 0;
			}
			return *(int*)(&num);
		}

		// Token: 0x06001324 RID: 4900 RVA: 0x000385CC File Offset: 0x000367CC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return Number.FormatSingle(this, null, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06001325 RID: 4901 RVA: 0x000385DB File Offset: 0x000367DB
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(IFormatProvider provider)
		{
			return Number.FormatSingle(this, null, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06001326 RID: 4902 RVA: 0x000385EB File Offset: 0x000367EB
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(string format)
		{
			return Number.FormatSingle(this, format, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06001327 RID: 4903 RVA: 0x000385FA File Offset: 0x000367FA
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(string format, IFormatProvider provider)
		{
			return Number.FormatSingle(this, format, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x06001328 RID: 4904 RVA: 0x0003860A File Offset: 0x0003680A
		[__DynamicallyInvokable]
		public static float Parse(string s)
		{
			return float.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x06001329 RID: 4905 RVA: 0x0003861C File Offset: 0x0003681C
		[__DynamicallyInvokable]
		public static float Parse(string s, NumberStyles style)
		{
			NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
			return float.Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x0600132A RID: 4906 RVA: 0x00038630 File Offset: 0x00036830
		[__DynamicallyInvokable]
		public static float Parse(string s, IFormatProvider provider)
		{
			return float.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x0600132B RID: 4907 RVA: 0x00038643 File Offset: 0x00036843
		[__DynamicallyInvokable]
		public static float Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
			return float.Parse(s, style, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x0600132C RID: 4908 RVA: 0x00038658 File Offset: 0x00036858
		private static float Parse(string s, NumberStyles style, NumberFormatInfo info)
		{
			return Number.ParseSingle(s, style, info);
		}

		// Token: 0x0600132D RID: 4909 RVA: 0x00038662 File Offset: 0x00036862
		[__DynamicallyInvokable]
		public static bool TryParse(string s, out float result)
		{
			return float.TryParse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, NumberFormatInfo.CurrentInfo, out result);
		}

		// Token: 0x0600132E RID: 4910 RVA: 0x00038675 File Offset: 0x00036875
		[__DynamicallyInvokable]
		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out float result)
		{
			NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
			return float.TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
		}

		// Token: 0x0600132F RID: 4911 RVA: 0x0003868C File Offset: 0x0003688C
		private static bool TryParse(string s, NumberStyles style, NumberFormatInfo info, out float result)
		{
			if (s == null)
			{
				result = 0f;
				return false;
			}
			if (!Number.TryParseSingle(s, style, info, out result))
			{
				string text = s.Trim();
				if (text.Equals(info.PositiveInfinitySymbol))
				{
					result = float.PositiveInfinity;
				}
				else if (text.Equals(info.NegativeInfinitySymbol))
				{
					result = float.NegativeInfinity;
				}
				else
				{
					if (!text.Equals(info.NaNSymbol))
					{
						return false;
					}
					result = float.NaN;
				}
			}
			return true;
		}

		// Token: 0x06001330 RID: 4912 RVA: 0x00038701 File Offset: 0x00036901
		public TypeCode GetTypeCode()
		{
			return TypeCode.Single;
		}

		// Token: 0x06001331 RID: 4913 RVA: 0x00038705 File Offset: 0x00036905
		[__DynamicallyInvokable]
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return Convert.ToBoolean(this);
		}

		// Token: 0x06001332 RID: 4914 RVA: 0x0003870E File Offset: 0x0003690E
		[__DynamicallyInvokable]
		char IConvertible.ToChar(IFormatProvider provider)
		{
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[] { "Single", "Char" }));
		}

		// Token: 0x06001333 RID: 4915 RVA: 0x00038735 File Offset: 0x00036935
		[__DynamicallyInvokable]
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return Convert.ToSByte(this);
		}

		// Token: 0x06001334 RID: 4916 RVA: 0x0003873E File Offset: 0x0003693E
		[__DynamicallyInvokable]
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return Convert.ToByte(this);
		}

		// Token: 0x06001335 RID: 4917 RVA: 0x00038747 File Offset: 0x00036947
		[__DynamicallyInvokable]
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return Convert.ToInt16(this);
		}

		// Token: 0x06001336 RID: 4918 RVA: 0x00038750 File Offset: 0x00036950
		[__DynamicallyInvokable]
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return Convert.ToUInt16(this);
		}

		// Token: 0x06001337 RID: 4919 RVA: 0x00038759 File Offset: 0x00036959
		[__DynamicallyInvokable]
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return Convert.ToInt32(this);
		}

		// Token: 0x06001338 RID: 4920 RVA: 0x00038762 File Offset: 0x00036962
		[__DynamicallyInvokable]
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32(this);
		}

		// Token: 0x06001339 RID: 4921 RVA: 0x0003876B File Offset: 0x0003696B
		[__DynamicallyInvokable]
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return Convert.ToInt64(this);
		}

		// Token: 0x0600133A RID: 4922 RVA: 0x00038774 File Offset: 0x00036974
		[__DynamicallyInvokable]
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return Convert.ToUInt64(this);
		}

		// Token: 0x0600133B RID: 4923 RVA: 0x0003877D File Offset: 0x0003697D
		[__DynamicallyInvokable]
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return this;
		}

		// Token: 0x0600133C RID: 4924 RVA: 0x00038781 File Offset: 0x00036981
		[__DynamicallyInvokable]
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(this);
		}

		// Token: 0x0600133D RID: 4925 RVA: 0x0003878A File Offset: 0x0003698A
		[__DynamicallyInvokable]
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return Convert.ToDecimal(this);
		}

		// Token: 0x0600133E RID: 4926 RVA: 0x00038793 File Offset: 0x00036993
		[__DynamicallyInvokable]
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[] { "Single", "DateTime" }));
		}

		// Token: 0x0600133F RID: 4927 RVA: 0x000387BA File Offset: 0x000369BA
		[__DynamicallyInvokable]
		object IConvertible.ToType(Type type, IFormatProvider provider)
		{
			return Convert.DefaultToType(this, type, provider);
		}

		// Token: 0x04000688 RID: 1672
		internal float m_value;

		// Token: 0x04000689 RID: 1673
		[__DynamicallyInvokable]
		public const float MinValue = -3.4028235E+38f;

		// Token: 0x0400068A RID: 1674
		[__DynamicallyInvokable]
		public const float Epsilon = 1E-45f;

		// Token: 0x0400068B RID: 1675
		[__DynamicallyInvokable]
		public const float MaxValue = 3.4028235E+38f;

		// Token: 0x0400068C RID: 1676
		[__DynamicallyInvokable]
		public const float PositiveInfinity = float.PositiveInfinity;

		// Token: 0x0400068D RID: 1677
		[__DynamicallyInvokable]
		public const float NegativeInfinity = float.NegativeInfinity;

		// Token: 0x0400068E RID: 1678
		[__DynamicallyInvokable]
		public const float NaN = float.NaN;
	}
}
