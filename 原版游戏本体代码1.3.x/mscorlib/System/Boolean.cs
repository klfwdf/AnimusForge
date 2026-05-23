using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System
{
	// Token: 0x020000B2 RID: 178
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct Boolean : IComparable, IConvertible, IComparable<bool>, IEquatable<bool>
	{
		// Token: 0x06000A35 RID: 2613 RVA: 0x00020FD1 File Offset: 0x0001F1D1
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			if (!this)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x06000A36 RID: 2614 RVA: 0x00020FDA File Offset: 0x0001F1DA
		[__DynamicallyInvokable]
		public override string ToString()
		{
			if (!this)
			{
				return "False";
			}
			return "True";
		}

		// Token: 0x06000A37 RID: 2615 RVA: 0x00020FEB File Offset: 0x0001F1EB
		public string ToString(IFormatProvider provider)
		{
			if (!this)
			{
				return "False";
			}
			return "True";
		}

		// Token: 0x06000A38 RID: 2616 RVA: 0x00020FFC File Offset: 0x0001F1FC
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is bool && this == (bool)obj;
		}

		// Token: 0x06000A39 RID: 2617 RVA: 0x00021012 File Offset: 0x0001F212
		[NonVersionable]
		[__DynamicallyInvokable]
		public bool Equals(bool obj)
		{
			return this == obj;
		}

		// Token: 0x06000A3A RID: 2618 RVA: 0x00021019 File Offset: 0x0001F219
		public int CompareTo(object obj)
		{
			if (obj == null)
			{
				return 1;
			}
			if (!(obj is bool))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeBoolean"));
			}
			if (this == (bool)obj)
			{
				return 0;
			}
			if (!this)
			{
				return -1;
			}
			return 1;
		}

		// Token: 0x06000A3B RID: 2619 RVA: 0x0002104B File Offset: 0x0001F24B
		[__DynamicallyInvokable]
		public int CompareTo(bool value)
		{
			if (this == value)
			{
				return 0;
			}
			if (!this)
			{
				return -1;
			}
			return 1;
		}

		// Token: 0x06000A3C RID: 2620 RVA: 0x0002105C File Offset: 0x0001F25C
		[__DynamicallyInvokable]
		public static bool Parse(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			bool result = false;
			if (!bool.TryParse(value, out result))
			{
				throw new FormatException(Environment.GetResourceString("Format_BadBoolean"));
			}
			return result;
		}

		// Token: 0x06000A3D RID: 2621 RVA: 0x00021094 File Offset: 0x0001F294
		[__DynamicallyInvokable]
		public static bool TryParse(string value, out bool result)
		{
			result = false;
			if (value == null)
			{
				return false;
			}
			if ("True".Equals(value, StringComparison.OrdinalIgnoreCase))
			{
				result = true;
				return true;
			}
			if ("False".Equals(value, StringComparison.OrdinalIgnoreCase))
			{
				result = false;
				return true;
			}
			value = bool.TrimWhiteSpaceAndNull(value);
			if ("True".Equals(value, StringComparison.OrdinalIgnoreCase))
			{
				result = true;
				return true;
			}
			if ("False".Equals(value, StringComparison.OrdinalIgnoreCase))
			{
				result = false;
				return true;
			}
			return false;
		}

		// Token: 0x06000A3E RID: 2622 RVA: 0x00021100 File Offset: 0x0001F300
		private static string TrimWhiteSpaceAndNull(string value)
		{
			int i = 0;
			int num = value.Length - 1;
			char c = '\0';
			while (i < value.Length)
			{
				if (!char.IsWhiteSpace(value[i]) && value[i] != c)
				{
					IL_52:
					while (num >= i && (char.IsWhiteSpace(value[num]) || value[num] == c))
					{
						num--;
					}
					return value.Substring(i, num - i + 1);
				}
				i++;
			}
			goto IL_52;
		}

		// Token: 0x06000A3F RID: 2623 RVA: 0x0002116F File Offset: 0x0001F36F
		public TypeCode GetTypeCode()
		{
			return TypeCode.Boolean;
		}

		// Token: 0x06000A40 RID: 2624 RVA: 0x00021172 File Offset: 0x0001F372
		[__DynamicallyInvokable]
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return this;
		}

		// Token: 0x06000A41 RID: 2625 RVA: 0x00021176 File Offset: 0x0001F376
		[__DynamicallyInvokable]
		char IConvertible.ToChar(IFormatProvider provider)
		{
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[] { "Boolean", "Char" }));
		}

		// Token: 0x06000A42 RID: 2626 RVA: 0x0002119D File Offset: 0x0001F39D
		[__DynamicallyInvokable]
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return Convert.ToSByte(this);
		}

		// Token: 0x06000A43 RID: 2627 RVA: 0x000211A6 File Offset: 0x0001F3A6
		[__DynamicallyInvokable]
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return Convert.ToByte(this);
		}

		// Token: 0x06000A44 RID: 2628 RVA: 0x000211AF File Offset: 0x0001F3AF
		[__DynamicallyInvokable]
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return Convert.ToInt16(this);
		}

		// Token: 0x06000A45 RID: 2629 RVA: 0x000211B8 File Offset: 0x0001F3B8
		[__DynamicallyInvokable]
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return Convert.ToUInt16(this);
		}

		// Token: 0x06000A46 RID: 2630 RVA: 0x000211C1 File Offset: 0x0001F3C1
		[__DynamicallyInvokable]
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return Convert.ToInt32(this);
		}

		// Token: 0x06000A47 RID: 2631 RVA: 0x000211CA File Offset: 0x0001F3CA
		[__DynamicallyInvokable]
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32(this);
		}

		// Token: 0x06000A48 RID: 2632 RVA: 0x000211D3 File Offset: 0x0001F3D3
		[__DynamicallyInvokable]
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return Convert.ToInt64(this);
		}

		// Token: 0x06000A49 RID: 2633 RVA: 0x000211DC File Offset: 0x0001F3DC
		[__DynamicallyInvokable]
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return Convert.ToUInt64(this);
		}

		// Token: 0x06000A4A RID: 2634 RVA: 0x000211E5 File Offset: 0x0001F3E5
		[__DynamicallyInvokable]
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return Convert.ToSingle(this);
		}

		// Token: 0x06000A4B RID: 2635 RVA: 0x000211EE File Offset: 0x0001F3EE
		[__DynamicallyInvokable]
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(this);
		}

		// Token: 0x06000A4C RID: 2636 RVA: 0x000211F7 File Offset: 0x0001F3F7
		[__DynamicallyInvokable]
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return Convert.ToDecimal(this);
		}

		// Token: 0x06000A4D RID: 2637 RVA: 0x00021200 File Offset: 0x0001F400
		[__DynamicallyInvokable]
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[] { "Boolean", "DateTime" }));
		}

		// Token: 0x06000A4E RID: 2638 RVA: 0x00021227 File Offset: 0x0001F427
		[__DynamicallyInvokable]
		object IConvertible.ToType(Type type, IFormatProvider provider)
		{
			return Convert.DefaultToType(this, type, provider);
		}

		// Token: 0x040003ED RID: 1005
		private bool m_value;

		// Token: 0x040003EE RID: 1006
		internal const int True = 1;

		// Token: 0x040003EF RID: 1007
		internal const int False = 0;

		// Token: 0x040003F0 RID: 1008
		internal const string TrueLiteral = "True";

		// Token: 0x040003F1 RID: 1009
		internal const string FalseLiteral = "False";

		// Token: 0x040003F2 RID: 1010
		[__DynamicallyInvokable]
		public static readonly string TrueString = "True";

		// Token: 0x040003F3 RID: 1011
		[__DynamicallyInvokable]
		public static readonly string FalseString = "False";
	}
}
