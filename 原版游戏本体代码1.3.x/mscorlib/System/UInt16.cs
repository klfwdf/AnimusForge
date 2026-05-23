using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace System
{
	// Token: 0x02000150 RID: 336
	[CLSCompliant(false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct UInt16 : IComparable, IFormattable, IConvertible, IComparable<ushort>, IEquatable<ushort>
	{
		// Token: 0x060014E2 RID: 5346 RVA: 0x0003DE95 File Offset: 0x0003C095
		public int CompareTo(object value)
		{
			if (value == null)
			{
				return 1;
			}
			if (value is ushort)
			{
				return (int)(this - (ushort)value);
			}
			throw new ArgumentException(Environment.GetResourceString("Arg_MustBeUInt16"));
		}

		// Token: 0x060014E3 RID: 5347 RVA: 0x0003DEBD File Offset: 0x0003C0BD
		[__DynamicallyInvokable]
		public int CompareTo(ushort value)
		{
			return (int)(this - value);
		}

		// Token: 0x060014E4 RID: 5348 RVA: 0x0003DEC3 File Offset: 0x0003C0C3
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is ushort && this == (ushort)obj;
		}

		// Token: 0x060014E5 RID: 5349 RVA: 0x0003DED9 File Offset: 0x0003C0D9
		[NonVersionable]
		[__DynamicallyInvokable]
		public bool Equals(ushort obj)
		{
			return this == obj;
		}

		// Token: 0x060014E6 RID: 5350 RVA: 0x0003DEE0 File Offset: 0x0003C0E0
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return (int)this;
		}

		// Token: 0x060014E7 RID: 5351 RVA: 0x0003DEE4 File Offset: 0x0003C0E4
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return Number.FormatUInt32((uint)this, null, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x060014E8 RID: 5352 RVA: 0x0003DEF3 File Offset: 0x0003C0F3
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(IFormatProvider provider)
		{
			return Number.FormatUInt32((uint)this, null, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x060014E9 RID: 5353 RVA: 0x0003DF03 File Offset: 0x0003C103
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(string format)
		{
			return Number.FormatUInt32((uint)this, format, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x060014EA RID: 5354 RVA: 0x0003DF12 File Offset: 0x0003C112
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(string format, IFormatProvider provider)
		{
			return Number.FormatUInt32((uint)this, format, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x060014EB RID: 5355 RVA: 0x0003DF22 File Offset: 0x0003C122
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort Parse(string s)
		{
			return ushort.Parse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x060014EC RID: 5356 RVA: 0x0003DF30 File Offset: 0x0003C130
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort Parse(string s, NumberStyles style)
		{
			NumberFormatInfo.ValidateParseStyleInteger(style);
			return ushort.Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		// Token: 0x060014ED RID: 5357 RVA: 0x0003DF44 File Offset: 0x0003C144
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort Parse(string s, IFormatProvider provider)
		{
			return ushort.Parse(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x060014EE RID: 5358 RVA: 0x0003DF53 File Offset: 0x0003C153
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			NumberFormatInfo.ValidateParseStyleInteger(style);
			return ushort.Parse(s, style, NumberFormatInfo.GetInstance(provider));
		}

		// Token: 0x060014EF RID: 5359 RVA: 0x0003DF68 File Offset: 0x0003C168
		private static ushort Parse(string s, NumberStyles style, NumberFormatInfo info)
		{
			uint num = 0U;
			try
			{
				num = Number.ParseUInt32(s, style, info);
			}
			catch (OverflowException innerException)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"), innerException);
			}
			if (num > 65535U)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
			}
			return (ushort)num;
		}

		// Token: 0x060014F0 RID: 5360 RVA: 0x0003DFC0 File Offset: 0x0003C1C0
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static bool TryParse(string s, out ushort result)
		{
			return ushort.TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		// Token: 0x060014F1 RID: 5361 RVA: 0x0003DFCF File Offset: 0x0003C1CF
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out ushort result)
		{
			NumberFormatInfo.ValidateParseStyleInteger(style);
			return ushort.TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
		}

		// Token: 0x060014F2 RID: 5362 RVA: 0x0003DFE8 File Offset: 0x0003C1E8
		private static bool TryParse(string s, NumberStyles style, NumberFormatInfo info, out ushort result)
		{
			result = 0;
			uint num;
			if (!Number.TryParseUInt32(s, style, info, out num))
			{
				return false;
			}
			if (num > 65535U)
			{
				return false;
			}
			result = (ushort)num;
			return true;
		}

		// Token: 0x060014F3 RID: 5363 RVA: 0x0003E015 File Offset: 0x0003C215
		public TypeCode GetTypeCode()
		{
			return TypeCode.UInt16;
		}

		// Token: 0x060014F4 RID: 5364 RVA: 0x0003E018 File Offset: 0x0003C218
		[__DynamicallyInvokable]
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return Convert.ToBoolean(this);
		}

		// Token: 0x060014F5 RID: 5365 RVA: 0x0003E021 File Offset: 0x0003C221
		[__DynamicallyInvokable]
		char IConvertible.ToChar(IFormatProvider provider)
		{
			return Convert.ToChar(this);
		}

		// Token: 0x060014F6 RID: 5366 RVA: 0x0003E02A File Offset: 0x0003C22A
		[__DynamicallyInvokable]
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return Convert.ToSByte(this);
		}

		// Token: 0x060014F7 RID: 5367 RVA: 0x0003E033 File Offset: 0x0003C233
		[__DynamicallyInvokable]
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return Convert.ToByte(this);
		}

		// Token: 0x060014F8 RID: 5368 RVA: 0x0003E03C File Offset: 0x0003C23C
		[__DynamicallyInvokable]
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return Convert.ToInt16(this);
		}

		// Token: 0x060014F9 RID: 5369 RVA: 0x0003E045 File Offset: 0x0003C245
		[__DynamicallyInvokable]
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return this;
		}

		// Token: 0x060014FA RID: 5370 RVA: 0x0003E049 File Offset: 0x0003C249
		[__DynamicallyInvokable]
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return Convert.ToInt32(this);
		}

		// Token: 0x060014FB RID: 5371 RVA: 0x0003E052 File Offset: 0x0003C252
		[__DynamicallyInvokable]
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32(this);
		}

		// Token: 0x060014FC RID: 5372 RVA: 0x0003E05B File Offset: 0x0003C25B
		[__DynamicallyInvokable]
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return Convert.ToInt64(this);
		}

		// Token: 0x060014FD RID: 5373 RVA: 0x0003E064 File Offset: 0x0003C264
		[__DynamicallyInvokable]
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return Convert.ToUInt64(this);
		}

		// Token: 0x060014FE RID: 5374 RVA: 0x0003E06D File Offset: 0x0003C26D
		[__DynamicallyInvokable]
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return Convert.ToSingle(this);
		}

		// Token: 0x060014FF RID: 5375 RVA: 0x0003E076 File Offset: 0x0003C276
		[__DynamicallyInvokable]
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(this);
		}

		// Token: 0x06001500 RID: 5376 RVA: 0x0003E07F File Offset: 0x0003C27F
		[__DynamicallyInvokable]
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return Convert.ToDecimal(this);
		}

		// Token: 0x06001501 RID: 5377 RVA: 0x0003E088 File Offset: 0x0003C288
		[__DynamicallyInvokable]
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[] { "UInt16", "DateTime" }));
		}

		// Token: 0x06001502 RID: 5378 RVA: 0x0003E0AF File Offset: 0x0003C2AF
		[__DynamicallyInvokable]
		object IConvertible.ToType(Type type, IFormatProvider provider)
		{
			return Convert.DefaultToType(this, type, provider);
		}

		// Token: 0x040006F1 RID: 1777
		private ushort m_value;

		// Token: 0x040006F2 RID: 1778
		[__DynamicallyInvokable]
		public const ushort MaxValue = 65535;

		// Token: 0x040006F3 RID: 1779
		[__DynamicallyInvokable]
		public const ushort MinValue = 0;
	}
}
