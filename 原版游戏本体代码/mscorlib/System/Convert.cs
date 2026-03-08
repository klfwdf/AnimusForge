using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace System
{
	// Token: 0x020000CC RID: 204
	[__DynamicallyInvokable]
	public static class Convert
	{
		// Token: 0x06000BA9 RID: 2985 RVA: 0x00025110 File Offset: 0x00023310
		[__DynamicallyInvokable]
		public static TypeCode GetTypeCode(object value)
		{
			if (value == null)
			{
				return TypeCode.Empty;
			}
			IConvertible convertible = value as IConvertible;
			if (convertible != null)
			{
				return convertible.GetTypeCode();
			}
			return TypeCode.Object;
		}

		// Token: 0x06000BAA RID: 2986 RVA: 0x00025134 File Offset: 0x00023334
		public static bool IsDBNull(object value)
		{
			if (value == System.DBNull.Value)
			{
				return true;
			}
			IConvertible convertible = value as IConvertible;
			return convertible != null && convertible.GetTypeCode() == TypeCode.DBNull;
		}

		// Token: 0x06000BAB RID: 2987 RVA: 0x00025160 File Offset: 0x00023360
		public static object ChangeType(object value, TypeCode typeCode)
		{
			return Convert.ChangeType(value, typeCode, Thread.CurrentThread.CurrentCulture);
		}

		// Token: 0x06000BAC RID: 2988 RVA: 0x00025174 File Offset: 0x00023374
		[__DynamicallyInvokable]
		public static object ChangeType(object value, TypeCode typeCode, IFormatProvider provider)
		{
			if (value == null && (typeCode == TypeCode.Empty || typeCode == TypeCode.String || typeCode == TypeCode.Object))
			{
				return null;
			}
			IConvertible convertible = value as IConvertible;
			if (convertible == null)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_IConvertible"));
			}
			switch (typeCode)
			{
			case TypeCode.Empty:
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_Empty"));
			case TypeCode.Object:
				return value;
			case TypeCode.DBNull:
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_DBNull"));
			case TypeCode.Boolean:
				return convertible.ToBoolean(provider);
			case TypeCode.Char:
				return convertible.ToChar(provider);
			case TypeCode.SByte:
				return convertible.ToSByte(provider);
			case TypeCode.Byte:
				return convertible.ToByte(provider);
			case TypeCode.Int16:
				return convertible.ToInt16(provider);
			case TypeCode.UInt16:
				return convertible.ToUInt16(provider);
			case TypeCode.Int32:
				return convertible.ToInt32(provider);
			case TypeCode.UInt32:
				return convertible.ToUInt32(provider);
			case TypeCode.Int64:
				return convertible.ToInt64(provider);
			case TypeCode.UInt64:
				return convertible.ToUInt64(provider);
			case TypeCode.Single:
				return convertible.ToSingle(provider);
			case TypeCode.Double:
				return convertible.ToDouble(provider);
			case TypeCode.Decimal:
				return convertible.ToDecimal(provider);
			case TypeCode.DateTime:
				return convertible.ToDateTime(provider);
			case TypeCode.String:
				return convertible.ToString(provider);
			}
			throw new ArgumentException(Environment.GetResourceString("Arg_UnknownTypeCode"));
		}

		// Token: 0x06000BAD RID: 2989 RVA: 0x000252F4 File Offset: 0x000234F4
		internal static object DefaultToType(IConvertible value, Type targetType, IFormatProvider provider)
		{
			if (targetType == null)
			{
				throw new ArgumentNullException("targetType");
			}
			RuntimeType left = targetType as RuntimeType;
			if (left != null)
			{
				if (value.GetType() == targetType)
				{
					return value;
				}
				if (left == Convert.ConvertTypes[3])
				{
					return value.ToBoolean(provider);
				}
				if (left == Convert.ConvertTypes[4])
				{
					return value.ToChar(provider);
				}
				if (left == Convert.ConvertTypes[5])
				{
					return value.ToSByte(provider);
				}
				if (left == Convert.ConvertTypes[6])
				{
					return value.ToByte(provider);
				}
				if (left == Convert.ConvertTypes[7])
				{
					return value.ToInt16(provider);
				}
				if (left == Convert.ConvertTypes[8])
				{
					return value.ToUInt16(provider);
				}
				if (left == Convert.ConvertTypes[9])
				{
					return value.ToInt32(provider);
				}
				if (left == Convert.ConvertTypes[10])
				{
					return value.ToUInt32(provider);
				}
				if (left == Convert.ConvertTypes[11])
				{
					return value.ToInt64(provider);
				}
				if (left == Convert.ConvertTypes[12])
				{
					return value.ToUInt64(provider);
				}
				if (left == Convert.ConvertTypes[13])
				{
					return value.ToSingle(provider);
				}
				if (left == Convert.ConvertTypes[14])
				{
					return value.ToDouble(provider);
				}
				if (left == Convert.ConvertTypes[15])
				{
					return value.ToDecimal(provider);
				}
				if (left == Convert.ConvertTypes[16])
				{
					return value.ToDateTime(provider);
				}
				if (left == Convert.ConvertTypes[18])
				{
					return value.ToString(provider);
				}
				if (left == Convert.ConvertTypes[1])
				{
					return value;
				}
				if (left == Convert.EnumType)
				{
					return (Enum)value;
				}
				if (left == Convert.ConvertTypes[2])
				{
					throw new InvalidCastException(Environment.GetResourceString("InvalidCast_DBNull"));
				}
				if (left == Convert.ConvertTypes[0])
				{
					throw new InvalidCastException(Environment.GetResourceString("InvalidCast_Empty"));
				}
			}
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[]
			{
				value.GetType().FullName,
				targetType.FullName
			}));
		}

		// Token: 0x06000BAE RID: 2990 RVA: 0x0002556F File Offset: 0x0002376F
		[__DynamicallyInvokable]
		public static object ChangeType(object value, Type conversionType)
		{
			return Convert.ChangeType(value, conversionType, Thread.CurrentThread.CurrentCulture);
		}

		// Token: 0x06000BAF RID: 2991 RVA: 0x00025584 File Offset: 0x00023784
		[__DynamicallyInvokable]
		public static object ChangeType(object value, Type conversionType, IFormatProvider provider)
		{
			if (conversionType == null)
			{
				throw new ArgumentNullException("conversionType");
			}
			if (value == null)
			{
				if (conversionType.IsValueType)
				{
					throw new InvalidCastException(Environment.GetResourceString("InvalidCast_CannotCastNullToValueType"));
				}
				return null;
			}
			else
			{
				IConvertible convertible = value as IConvertible;
				if (convertible == null)
				{
					if (value.GetType() == conversionType)
					{
						return value;
					}
					throw new InvalidCastException(Environment.GetResourceString("InvalidCast_IConvertible"));
				}
				else
				{
					RuntimeType left = conversionType as RuntimeType;
					if (left == Convert.ConvertTypes[3])
					{
						return convertible.ToBoolean(provider);
					}
					if (left == Convert.ConvertTypes[4])
					{
						return convertible.ToChar(provider);
					}
					if (left == Convert.ConvertTypes[5])
					{
						return convertible.ToSByte(provider);
					}
					if (left == Convert.ConvertTypes[6])
					{
						return convertible.ToByte(provider);
					}
					if (left == Convert.ConvertTypes[7])
					{
						return convertible.ToInt16(provider);
					}
					if (left == Convert.ConvertTypes[8])
					{
						return convertible.ToUInt16(provider);
					}
					if (left == Convert.ConvertTypes[9])
					{
						return convertible.ToInt32(provider);
					}
					if (left == Convert.ConvertTypes[10])
					{
						return convertible.ToUInt32(provider);
					}
					if (left == Convert.ConvertTypes[11])
					{
						return convertible.ToInt64(provider);
					}
					if (left == Convert.ConvertTypes[12])
					{
						return convertible.ToUInt64(provider);
					}
					if (left == Convert.ConvertTypes[13])
					{
						return convertible.ToSingle(provider);
					}
					if (left == Convert.ConvertTypes[14])
					{
						return convertible.ToDouble(provider);
					}
					if (left == Convert.ConvertTypes[15])
					{
						return convertible.ToDecimal(provider);
					}
					if (left == Convert.ConvertTypes[16])
					{
						return convertible.ToDateTime(provider);
					}
					if (left == Convert.ConvertTypes[18])
					{
						return convertible.ToString(provider);
					}
					if (left == Convert.ConvertTypes[1])
					{
						return value;
					}
					return convertible.ToType(conversionType, provider);
				}
			}
		}

		// Token: 0x06000BB0 RID: 2992 RVA: 0x000257B4 File Offset: 0x000239B4
		[__DynamicallyInvokable]
		public static bool ToBoolean(object value)
		{
			return value != null && ((IConvertible)value).ToBoolean(null);
		}

		// Token: 0x06000BB1 RID: 2993 RVA: 0x000257C7 File Offset: 0x000239C7
		[__DynamicallyInvokable]
		public static bool ToBoolean(object value, IFormatProvider provider)
		{
			return value != null && ((IConvertible)value).ToBoolean(provider);
		}

		// Token: 0x06000BB2 RID: 2994 RVA: 0x000257DA File Offset: 0x000239DA
		[__DynamicallyInvokable]
		public static bool ToBoolean(bool value)
		{
			return value;
		}

		// Token: 0x06000BB3 RID: 2995 RVA: 0x000257DD File Offset: 0x000239DD
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static bool ToBoolean(sbyte value)
		{
			return value != 0;
		}

		// Token: 0x06000BB4 RID: 2996 RVA: 0x000257E3 File Offset: 0x000239E3
		public static bool ToBoolean(char value)
		{
			return ((IConvertible)value).ToBoolean(null);
		}

		// Token: 0x06000BB5 RID: 2997 RVA: 0x000257F1 File Offset: 0x000239F1
		[__DynamicallyInvokable]
		public static bool ToBoolean(byte value)
		{
			return value > 0;
		}

		// Token: 0x06000BB6 RID: 2998 RVA: 0x000257F7 File Offset: 0x000239F7
		[__DynamicallyInvokable]
		public static bool ToBoolean(short value)
		{
			return value != 0;
		}

		// Token: 0x06000BB7 RID: 2999 RVA: 0x000257FD File Offset: 0x000239FD
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static bool ToBoolean(ushort value)
		{
			return value > 0;
		}

		// Token: 0x06000BB8 RID: 3000 RVA: 0x00025803 File Offset: 0x00023A03
		[__DynamicallyInvokable]
		public static bool ToBoolean(int value)
		{
			return value != 0;
		}

		// Token: 0x06000BB9 RID: 3001 RVA: 0x00025809 File Offset: 0x00023A09
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static bool ToBoolean(uint value)
		{
			return value > 0U;
		}

		// Token: 0x06000BBA RID: 3002 RVA: 0x0002580F File Offset: 0x00023A0F
		[__DynamicallyInvokable]
		public static bool ToBoolean(long value)
		{
			return value != 0L;
		}

		// Token: 0x06000BBB RID: 3003 RVA: 0x00025816 File Offset: 0x00023A16
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static bool ToBoolean(ulong value)
		{
			return value > 0UL;
		}

		// Token: 0x06000BBC RID: 3004 RVA: 0x0002581D File Offset: 0x00023A1D
		[__DynamicallyInvokable]
		public static bool ToBoolean(string value)
		{
			return value != null && bool.Parse(value);
		}

		// Token: 0x06000BBD RID: 3005 RVA: 0x0002582A File Offset: 0x00023A2A
		[__DynamicallyInvokable]
		public static bool ToBoolean(string value, IFormatProvider provider)
		{
			return value != null && bool.Parse(value);
		}

		// Token: 0x06000BBE RID: 3006 RVA: 0x00025837 File Offset: 0x00023A37
		[__DynamicallyInvokable]
		public static bool ToBoolean(float value)
		{
			return value != 0f;
		}

		// Token: 0x06000BBF RID: 3007 RVA: 0x00025844 File Offset: 0x00023A44
		[__DynamicallyInvokable]
		public static bool ToBoolean(double value)
		{
			return value != 0.0;
		}

		// Token: 0x06000BC0 RID: 3008 RVA: 0x00025855 File Offset: 0x00023A55
		[__DynamicallyInvokable]
		public static bool ToBoolean(decimal value)
		{
			return value != 0m;
		}

		// Token: 0x06000BC1 RID: 3009 RVA: 0x00025862 File Offset: 0x00023A62
		public static bool ToBoolean(DateTime value)
		{
			return ((IConvertible)value).ToBoolean(null);
		}

		// Token: 0x06000BC2 RID: 3010 RVA: 0x00025870 File Offset: 0x00023A70
		[__DynamicallyInvokable]
		public static char ToChar(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToChar(null);
			}
			return '\0';
		}

		// Token: 0x06000BC3 RID: 3011 RVA: 0x00025883 File Offset: 0x00023A83
		[__DynamicallyInvokable]
		public static char ToChar(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToChar(provider);
			}
			return '\0';
		}

		// Token: 0x06000BC4 RID: 3012 RVA: 0x00025896 File Offset: 0x00023A96
		public static char ToChar(bool value)
		{
			return ((IConvertible)value).ToChar(null);
		}

		// Token: 0x06000BC5 RID: 3013 RVA: 0x000258A4 File Offset: 0x00023AA4
		public static char ToChar(char value)
		{
			return value;
		}

		// Token: 0x06000BC6 RID: 3014 RVA: 0x000258A7 File Offset: 0x00023AA7
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static char ToChar(sbyte value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Char"));
			}
			return (char)value;
		}

		// Token: 0x06000BC7 RID: 3015 RVA: 0x000258BF File Offset: 0x00023ABF
		[__DynamicallyInvokable]
		public static char ToChar(byte value)
		{
			return (char)value;
		}

		// Token: 0x06000BC8 RID: 3016 RVA: 0x000258C2 File Offset: 0x00023AC2
		[__DynamicallyInvokable]
		public static char ToChar(short value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Char"));
			}
			return (char)value;
		}

		// Token: 0x06000BC9 RID: 3017 RVA: 0x000258DA File Offset: 0x00023ADA
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static char ToChar(ushort value)
		{
			return (char)value;
		}

		// Token: 0x06000BCA RID: 3018 RVA: 0x000258DD File Offset: 0x00023ADD
		[__DynamicallyInvokable]
		public static char ToChar(int value)
		{
			if (value < 0 || value > 65535)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Char"));
			}
			return (char)value;
		}

		// Token: 0x06000BCB RID: 3019 RVA: 0x000258FD File Offset: 0x00023AFD
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static char ToChar(uint value)
		{
			if (value > 65535U)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Char"));
			}
			return (char)value;
		}

		// Token: 0x06000BCC RID: 3020 RVA: 0x00025919 File Offset: 0x00023B19
		[__DynamicallyInvokable]
		public static char ToChar(long value)
		{
			if (value < 0L || value > 65535L)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Char"));
			}
			return (char)value;
		}

		// Token: 0x06000BCD RID: 3021 RVA: 0x0002593B File Offset: 0x00023B3B
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static char ToChar(ulong value)
		{
			if (value > 65535UL)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Char"));
			}
			return (char)value;
		}

		// Token: 0x06000BCE RID: 3022 RVA: 0x00025958 File Offset: 0x00023B58
		[__DynamicallyInvokable]
		public static char ToChar(string value)
		{
			return Convert.ToChar(value, null);
		}

		// Token: 0x06000BCF RID: 3023 RVA: 0x00025961 File Offset: 0x00023B61
		[__DynamicallyInvokable]
		public static char ToChar(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length != 1)
			{
				throw new FormatException(Environment.GetResourceString("Format_NeedSingleChar"));
			}
			return value[0];
		}

		// Token: 0x06000BD0 RID: 3024 RVA: 0x00025991 File Offset: 0x00023B91
		public static char ToChar(float value)
		{
			return ((IConvertible)value).ToChar(null);
		}

		// Token: 0x06000BD1 RID: 3025 RVA: 0x0002599F File Offset: 0x00023B9F
		public static char ToChar(double value)
		{
			return ((IConvertible)value).ToChar(null);
		}

		// Token: 0x06000BD2 RID: 3026 RVA: 0x000259AD File Offset: 0x00023BAD
		public static char ToChar(decimal value)
		{
			return ((IConvertible)value).ToChar(null);
		}

		// Token: 0x06000BD3 RID: 3027 RVA: 0x000259BB File Offset: 0x00023BBB
		public static char ToChar(DateTime value)
		{
			return ((IConvertible)value).ToChar(null);
		}

		// Token: 0x06000BD4 RID: 3028 RVA: 0x000259C9 File Offset: 0x00023BC9
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToSByte(null);
			}
			return 0;
		}

		// Token: 0x06000BD5 RID: 3029 RVA: 0x000259DC File Offset: 0x00023BDC
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToSByte(provider);
			}
			return 0;
		}

		// Token: 0x06000BD6 RID: 3030 RVA: 0x000259EF File Offset: 0x00023BEF
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(bool value)
		{
			if (!value)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x06000BD7 RID: 3031 RVA: 0x000259F7 File Offset: 0x00023BF7
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(sbyte value)
		{
			return value;
		}

		// Token: 0x06000BD8 RID: 3032 RVA: 0x000259FA File Offset: 0x00023BFA
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(char value)
		{
			if (value > '\u007f')
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
			}
			return (sbyte)value;
		}

		// Token: 0x06000BD9 RID: 3033 RVA: 0x00025A13 File Offset: 0x00023C13
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(byte value)
		{
			if (value > 127)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
			}
			return (sbyte)value;
		}

		// Token: 0x06000BDA RID: 3034 RVA: 0x00025A2C File Offset: 0x00023C2C
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(short value)
		{
			if (value < -128 || value > 127)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
			}
			return (sbyte)value;
		}

		// Token: 0x06000BDB RID: 3035 RVA: 0x00025A4A File Offset: 0x00023C4A
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(ushort value)
		{
			if (value > 127)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
			}
			return (sbyte)value;
		}

		// Token: 0x06000BDC RID: 3036 RVA: 0x00025A63 File Offset: 0x00023C63
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(int value)
		{
			if (value < -128 || value > 127)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
			}
			return (sbyte)value;
		}

		// Token: 0x06000BDD RID: 3037 RVA: 0x00025A81 File Offset: 0x00023C81
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(uint value)
		{
			if ((ulong)value > 127UL)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
			}
			return (sbyte)value;
		}

		// Token: 0x06000BDE RID: 3038 RVA: 0x00025A9C File Offset: 0x00023C9C
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(long value)
		{
			if (value < -128L || value > 127L)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
			}
			return (sbyte)value;
		}

		// Token: 0x06000BDF RID: 3039 RVA: 0x00025ABC File Offset: 0x00023CBC
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(ulong value)
		{
			if (value > 127UL)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
			}
			return (sbyte)value;
		}

		// Token: 0x06000BE0 RID: 3040 RVA: 0x00025AD6 File Offset: 0x00023CD6
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(float value)
		{
			return Convert.ToSByte((double)value);
		}

		// Token: 0x06000BE1 RID: 3041 RVA: 0x00025ADF File Offset: 0x00023CDF
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(double value)
		{
			return Convert.ToSByte(Convert.ToInt32(value));
		}

		// Token: 0x06000BE2 RID: 3042 RVA: 0x00025AEC File Offset: 0x00023CEC
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(decimal value)
		{
			return decimal.ToSByte(decimal.Round(value, 0));
		}

		// Token: 0x06000BE3 RID: 3043 RVA: 0x00025AFA File Offset: 0x00023CFA
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(string value)
		{
			if (value == null)
			{
				return 0;
			}
			return sbyte.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000BE4 RID: 3044 RVA: 0x00025B0C File Offset: 0x00023D0C
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(string value, IFormatProvider provider)
		{
			return sbyte.Parse(value, NumberStyles.Integer, provider);
		}

		// Token: 0x06000BE5 RID: 3045 RVA: 0x00025B16 File Offset: 0x00023D16
		[CLSCompliant(false)]
		public static sbyte ToSByte(DateTime value)
		{
			return ((IConvertible)value).ToSByte(null);
		}

		// Token: 0x06000BE6 RID: 3046 RVA: 0x00025B24 File Offset: 0x00023D24
		[__DynamicallyInvokable]
		public static byte ToByte(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToByte(null);
			}
			return 0;
		}

		// Token: 0x06000BE7 RID: 3047 RVA: 0x00025B37 File Offset: 0x00023D37
		[__DynamicallyInvokable]
		public static byte ToByte(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToByte(provider);
			}
			return 0;
		}

		// Token: 0x06000BE8 RID: 3048 RVA: 0x00025B4A File Offset: 0x00023D4A
		[__DynamicallyInvokable]
		public static byte ToByte(bool value)
		{
			if (!value)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x06000BE9 RID: 3049 RVA: 0x00025B52 File Offset: 0x00023D52
		[__DynamicallyInvokable]
		public static byte ToByte(byte value)
		{
			return value;
		}

		// Token: 0x06000BEA RID: 3050 RVA: 0x00025B55 File Offset: 0x00023D55
		[__DynamicallyInvokable]
		public static byte ToByte(char value)
		{
			if (value > 'ÿ')
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
			}
			return (byte)value;
		}

		// Token: 0x06000BEB RID: 3051 RVA: 0x00025B71 File Offset: 0x00023D71
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static byte ToByte(sbyte value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
			}
			return (byte)value;
		}

		// Token: 0x06000BEC RID: 3052 RVA: 0x00025B89 File Offset: 0x00023D89
		[__DynamicallyInvokable]
		public static byte ToByte(short value)
		{
			if (value < 0 || value > 255)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
			}
			return (byte)value;
		}

		// Token: 0x06000BED RID: 3053 RVA: 0x00025BA9 File Offset: 0x00023DA9
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static byte ToByte(ushort value)
		{
			if (value > 255)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
			}
			return (byte)value;
		}

		// Token: 0x06000BEE RID: 3054 RVA: 0x00025BC5 File Offset: 0x00023DC5
		[__DynamicallyInvokable]
		public static byte ToByte(int value)
		{
			if (value < 0 || value > 255)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
			}
			return (byte)value;
		}

		// Token: 0x06000BEF RID: 3055 RVA: 0x00025BE5 File Offset: 0x00023DE5
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static byte ToByte(uint value)
		{
			if (value > 255U)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
			}
			return (byte)value;
		}

		// Token: 0x06000BF0 RID: 3056 RVA: 0x00025C01 File Offset: 0x00023E01
		[__DynamicallyInvokable]
		public static byte ToByte(long value)
		{
			if (value < 0L || value > 255L)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
			}
			return (byte)value;
		}

		// Token: 0x06000BF1 RID: 3057 RVA: 0x00025C23 File Offset: 0x00023E23
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static byte ToByte(ulong value)
		{
			if (value > 255UL)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
			}
			return (byte)value;
		}

		// Token: 0x06000BF2 RID: 3058 RVA: 0x00025C40 File Offset: 0x00023E40
		[__DynamicallyInvokable]
		public static byte ToByte(float value)
		{
			return Convert.ToByte((double)value);
		}

		// Token: 0x06000BF3 RID: 3059 RVA: 0x00025C49 File Offset: 0x00023E49
		[__DynamicallyInvokable]
		public static byte ToByte(double value)
		{
			return Convert.ToByte(Convert.ToInt32(value));
		}

		// Token: 0x06000BF4 RID: 3060 RVA: 0x00025C56 File Offset: 0x00023E56
		[__DynamicallyInvokable]
		public static byte ToByte(decimal value)
		{
			return decimal.ToByte(decimal.Round(value, 0));
		}

		// Token: 0x06000BF5 RID: 3061 RVA: 0x00025C64 File Offset: 0x00023E64
		[__DynamicallyInvokable]
		public static byte ToByte(string value)
		{
			if (value == null)
			{
				return 0;
			}
			return byte.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000BF6 RID: 3062 RVA: 0x00025C76 File Offset: 0x00023E76
		[__DynamicallyInvokable]
		public static byte ToByte(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return 0;
			}
			return byte.Parse(value, NumberStyles.Integer, provider);
		}

		// Token: 0x06000BF7 RID: 3063 RVA: 0x00025C85 File Offset: 0x00023E85
		public static byte ToByte(DateTime value)
		{
			return ((IConvertible)value).ToByte(null);
		}

		// Token: 0x06000BF8 RID: 3064 RVA: 0x00025C93 File Offset: 0x00023E93
		[__DynamicallyInvokable]
		public static short ToInt16(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToInt16(null);
			}
			return 0;
		}

		// Token: 0x06000BF9 RID: 3065 RVA: 0x00025CA6 File Offset: 0x00023EA6
		[__DynamicallyInvokable]
		public static short ToInt16(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToInt16(provider);
			}
			return 0;
		}

		// Token: 0x06000BFA RID: 3066 RVA: 0x00025CB9 File Offset: 0x00023EB9
		[__DynamicallyInvokable]
		public static short ToInt16(bool value)
		{
			if (!value)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x06000BFB RID: 3067 RVA: 0x00025CC1 File Offset: 0x00023EC1
		[__DynamicallyInvokable]
		public static short ToInt16(char value)
		{
			if (value > '翿')
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
			}
			return (short)value;
		}

		// Token: 0x06000BFC RID: 3068 RVA: 0x00025CDD File Offset: 0x00023EDD
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static short ToInt16(sbyte value)
		{
			return (short)value;
		}

		// Token: 0x06000BFD RID: 3069 RVA: 0x00025CE0 File Offset: 0x00023EE0
		[__DynamicallyInvokable]
		public static short ToInt16(byte value)
		{
			return (short)value;
		}

		// Token: 0x06000BFE RID: 3070 RVA: 0x00025CE3 File Offset: 0x00023EE3
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static short ToInt16(ushort value)
		{
			if (value > 32767)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
			}
			return (short)value;
		}

		// Token: 0x06000BFF RID: 3071 RVA: 0x00025CFF File Offset: 0x00023EFF
		[__DynamicallyInvokable]
		public static short ToInt16(int value)
		{
			if (value < -32768 || value > 32767)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
			}
			return (short)value;
		}

		// Token: 0x06000C00 RID: 3072 RVA: 0x00025D23 File Offset: 0x00023F23
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static short ToInt16(uint value)
		{
			if ((ulong)value > 32767UL)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
			}
			return (short)value;
		}

		// Token: 0x06000C01 RID: 3073 RVA: 0x00025D41 File Offset: 0x00023F41
		[__DynamicallyInvokable]
		public static short ToInt16(short value)
		{
			return value;
		}

		// Token: 0x06000C02 RID: 3074 RVA: 0x00025D44 File Offset: 0x00023F44
		[__DynamicallyInvokable]
		public static short ToInt16(long value)
		{
			if (value < -32768L || value > 32767L)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
			}
			return (short)value;
		}

		// Token: 0x06000C03 RID: 3075 RVA: 0x00025D6A File Offset: 0x00023F6A
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static short ToInt16(ulong value)
		{
			if (value > 32767UL)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
			}
			return (short)value;
		}

		// Token: 0x06000C04 RID: 3076 RVA: 0x00025D87 File Offset: 0x00023F87
		[__DynamicallyInvokable]
		public static short ToInt16(float value)
		{
			return Convert.ToInt16((double)value);
		}

		// Token: 0x06000C05 RID: 3077 RVA: 0x00025D90 File Offset: 0x00023F90
		[__DynamicallyInvokable]
		public static short ToInt16(double value)
		{
			return Convert.ToInt16(Convert.ToInt32(value));
		}

		// Token: 0x06000C06 RID: 3078 RVA: 0x00025D9D File Offset: 0x00023F9D
		[__DynamicallyInvokable]
		public static short ToInt16(decimal value)
		{
			return decimal.ToInt16(decimal.Round(value, 0));
		}

		// Token: 0x06000C07 RID: 3079 RVA: 0x00025DAB File Offset: 0x00023FAB
		[__DynamicallyInvokable]
		public static short ToInt16(string value)
		{
			if (value == null)
			{
				return 0;
			}
			return short.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000C08 RID: 3080 RVA: 0x00025DBD File Offset: 0x00023FBD
		[__DynamicallyInvokable]
		public static short ToInt16(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return 0;
			}
			return short.Parse(value, NumberStyles.Integer, provider);
		}

		// Token: 0x06000C09 RID: 3081 RVA: 0x00025DCC File Offset: 0x00023FCC
		public static short ToInt16(DateTime value)
		{
			return ((IConvertible)value).ToInt16(null);
		}

		// Token: 0x06000C0A RID: 3082 RVA: 0x00025DDA File Offset: 0x00023FDA
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToUInt16(null);
			}
			return 0;
		}

		// Token: 0x06000C0B RID: 3083 RVA: 0x00025DED File Offset: 0x00023FED
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToUInt16(provider);
			}
			return 0;
		}

		// Token: 0x06000C0C RID: 3084 RVA: 0x00025E00 File Offset: 0x00024000
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(bool value)
		{
			if (!value)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x06000C0D RID: 3085 RVA: 0x00025E08 File Offset: 0x00024008
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(char value)
		{
			return (ushort)value;
		}

		// Token: 0x06000C0E RID: 3086 RVA: 0x00025E0B File Offset: 0x0002400B
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(sbyte value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
			}
			return (ushort)value;
		}

		// Token: 0x06000C0F RID: 3087 RVA: 0x00025E23 File Offset: 0x00024023
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(byte value)
		{
			return (ushort)value;
		}

		// Token: 0x06000C10 RID: 3088 RVA: 0x00025E26 File Offset: 0x00024026
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(short value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
			}
			return (ushort)value;
		}

		// Token: 0x06000C11 RID: 3089 RVA: 0x00025E3E File Offset: 0x0002403E
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(int value)
		{
			if (value < 0 || value > 65535)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
			}
			return (ushort)value;
		}

		// Token: 0x06000C12 RID: 3090 RVA: 0x00025E5E File Offset: 0x0002405E
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(ushort value)
		{
			return value;
		}

		// Token: 0x06000C13 RID: 3091 RVA: 0x00025E61 File Offset: 0x00024061
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(uint value)
		{
			if (value > 65535U)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
			}
			return (ushort)value;
		}

		// Token: 0x06000C14 RID: 3092 RVA: 0x00025E7D File Offset: 0x0002407D
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(long value)
		{
			if (value < 0L || value > 65535L)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
			}
			return (ushort)value;
		}

		// Token: 0x06000C15 RID: 3093 RVA: 0x00025E9F File Offset: 0x0002409F
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(ulong value)
		{
			if (value > 65535UL)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
			}
			return (ushort)value;
		}

		// Token: 0x06000C16 RID: 3094 RVA: 0x00025EBC File Offset: 0x000240BC
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(float value)
		{
			return Convert.ToUInt16((double)value);
		}

		// Token: 0x06000C17 RID: 3095 RVA: 0x00025EC5 File Offset: 0x000240C5
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(double value)
		{
			return Convert.ToUInt16(Convert.ToInt32(value));
		}

		// Token: 0x06000C18 RID: 3096 RVA: 0x00025ED2 File Offset: 0x000240D2
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(decimal value)
		{
			return decimal.ToUInt16(decimal.Round(value, 0));
		}

		// Token: 0x06000C19 RID: 3097 RVA: 0x00025EE0 File Offset: 0x000240E0
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(string value)
		{
			if (value == null)
			{
				return 0;
			}
			return ushort.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000C1A RID: 3098 RVA: 0x00025EF2 File Offset: 0x000240F2
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return 0;
			}
			return ushort.Parse(value, NumberStyles.Integer, provider);
		}

		// Token: 0x06000C1B RID: 3099 RVA: 0x00025F01 File Offset: 0x00024101
		[CLSCompliant(false)]
		public static ushort ToUInt16(DateTime value)
		{
			return ((IConvertible)value).ToUInt16(null);
		}

		// Token: 0x06000C1C RID: 3100 RVA: 0x00025F0F File Offset: 0x0002410F
		[__DynamicallyInvokable]
		public static int ToInt32(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToInt32(null);
			}
			return 0;
		}

		// Token: 0x06000C1D RID: 3101 RVA: 0x00025F22 File Offset: 0x00024122
		[__DynamicallyInvokable]
		public static int ToInt32(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToInt32(provider);
			}
			return 0;
		}

		// Token: 0x06000C1E RID: 3102 RVA: 0x00025F35 File Offset: 0x00024135
		[__DynamicallyInvokable]
		public static int ToInt32(bool value)
		{
			if (!value)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x06000C1F RID: 3103 RVA: 0x00025F3D File Offset: 0x0002413D
		[__DynamicallyInvokable]
		public static int ToInt32(char value)
		{
			return (int)value;
		}

		// Token: 0x06000C20 RID: 3104 RVA: 0x00025F40 File Offset: 0x00024140
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static int ToInt32(sbyte value)
		{
			return (int)value;
		}

		// Token: 0x06000C21 RID: 3105 RVA: 0x00025F43 File Offset: 0x00024143
		[__DynamicallyInvokable]
		public static int ToInt32(byte value)
		{
			return (int)value;
		}

		// Token: 0x06000C22 RID: 3106 RVA: 0x00025F46 File Offset: 0x00024146
		[__DynamicallyInvokable]
		public static int ToInt32(short value)
		{
			return (int)value;
		}

		// Token: 0x06000C23 RID: 3107 RVA: 0x00025F49 File Offset: 0x00024149
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static int ToInt32(ushort value)
		{
			return (int)value;
		}

		// Token: 0x06000C24 RID: 3108 RVA: 0x00025F4C File Offset: 0x0002414C
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static int ToInt32(uint value)
		{
			if (value > 2147483647U)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
			}
			return (int)value;
		}

		// Token: 0x06000C25 RID: 3109 RVA: 0x00025F67 File Offset: 0x00024167
		[__DynamicallyInvokable]
		public static int ToInt32(int value)
		{
			return value;
		}

		// Token: 0x06000C26 RID: 3110 RVA: 0x00025F6A File Offset: 0x0002416A
		[__DynamicallyInvokable]
		public static int ToInt32(long value)
		{
			if (value < -2147483648L || value > 2147483647L)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
			}
			return (int)value;
		}

		// Token: 0x06000C27 RID: 3111 RVA: 0x00025F90 File Offset: 0x00024190
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static int ToInt32(ulong value)
		{
			if (value > 2147483647UL)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
			}
			return (int)value;
		}

		// Token: 0x06000C28 RID: 3112 RVA: 0x00025FAD File Offset: 0x000241AD
		[__DynamicallyInvokable]
		public static int ToInt32(float value)
		{
			return Convert.ToInt32((double)value);
		}

		// Token: 0x06000C29 RID: 3113 RVA: 0x00025FB8 File Offset: 0x000241B8
		[__DynamicallyInvokable]
		public static int ToInt32(double value)
		{
			if (value >= 0.0)
			{
				if (value < 2147483647.5)
				{
					int num = (int)value;
					double num2 = value - (double)num;
					if (num2 > 0.5 || (num2 == 0.5 && (num & 1) != 0))
					{
						num++;
					}
					return num;
				}
			}
			else if (value >= -2147483648.5)
			{
				int num3 = (int)value;
				double num4 = value - (double)num3;
				if (num4 < -0.5 || (num4 == -0.5 && (num3 & 1) != 0))
				{
					num3--;
				}
				return num3;
			}
			throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
		}

		// Token: 0x06000C2A RID: 3114 RVA: 0x0002604E File Offset: 0x0002424E
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static int ToInt32(decimal value)
		{
			return decimal.FCallToInt32(value);
		}

		// Token: 0x06000C2B RID: 3115 RVA: 0x00026056 File Offset: 0x00024256
		[__DynamicallyInvokable]
		public static int ToInt32(string value)
		{
			if (value == null)
			{
				return 0;
			}
			return int.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000C2C RID: 3116 RVA: 0x00026068 File Offset: 0x00024268
		[__DynamicallyInvokable]
		public static int ToInt32(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return 0;
			}
			return int.Parse(value, NumberStyles.Integer, provider);
		}

		// Token: 0x06000C2D RID: 3117 RVA: 0x00026077 File Offset: 0x00024277
		public static int ToInt32(DateTime value)
		{
			return ((IConvertible)value).ToInt32(null);
		}

		// Token: 0x06000C2E RID: 3118 RVA: 0x00026085 File Offset: 0x00024285
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToUInt32(null);
			}
			return 0U;
		}

		// Token: 0x06000C2F RID: 3119 RVA: 0x00026098 File Offset: 0x00024298
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToUInt32(provider);
			}
			return 0U;
		}

		// Token: 0x06000C30 RID: 3120 RVA: 0x000260AB File Offset: 0x000242AB
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(bool value)
		{
			if (!value)
			{
				return 0U;
			}
			return 1U;
		}

		// Token: 0x06000C31 RID: 3121 RVA: 0x000260B3 File Offset: 0x000242B3
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(char value)
		{
			return (uint)value;
		}

		// Token: 0x06000C32 RID: 3122 RVA: 0x000260B6 File Offset: 0x000242B6
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(sbyte value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
			}
			return (uint)value;
		}

		// Token: 0x06000C33 RID: 3123 RVA: 0x000260CD File Offset: 0x000242CD
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(byte value)
		{
			return (uint)value;
		}

		// Token: 0x06000C34 RID: 3124 RVA: 0x000260D0 File Offset: 0x000242D0
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(short value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
			}
			return (uint)value;
		}

		// Token: 0x06000C35 RID: 3125 RVA: 0x000260E7 File Offset: 0x000242E7
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(ushort value)
		{
			return (uint)value;
		}

		// Token: 0x06000C36 RID: 3126 RVA: 0x000260EA File Offset: 0x000242EA
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(int value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
			}
			return (uint)value;
		}

		// Token: 0x06000C37 RID: 3127 RVA: 0x00026101 File Offset: 0x00024301
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(uint value)
		{
			return value;
		}

		// Token: 0x06000C38 RID: 3128 RVA: 0x00026104 File Offset: 0x00024304
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(long value)
		{
			if (value < 0L || value > (long)((ulong)(-1)))
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
			}
			return (uint)value;
		}

		// Token: 0x06000C39 RID: 3129 RVA: 0x00026122 File Offset: 0x00024322
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(ulong value)
		{
			if (value > (ulong)(-1))
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
			}
			return (uint)value;
		}

		// Token: 0x06000C3A RID: 3130 RVA: 0x0002613B File Offset: 0x0002433B
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(float value)
		{
			return Convert.ToUInt32((double)value);
		}

		// Token: 0x06000C3B RID: 3131 RVA: 0x00026144 File Offset: 0x00024344
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(double value)
		{
			if (value >= -0.5 && value < 4294967295.5)
			{
				uint num = (uint)value;
				double num2 = value - num;
				if (num2 > 0.5 || (num2 == 0.5 && (num & 1U) != 0U))
				{
					num += 1U;
				}
				return num;
			}
			throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
		}

		// Token: 0x06000C3C RID: 3132 RVA: 0x000261A4 File Offset: 0x000243A4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(decimal value)
		{
			return decimal.ToUInt32(decimal.Round(value, 0));
		}

		// Token: 0x06000C3D RID: 3133 RVA: 0x000261B2 File Offset: 0x000243B2
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(string value)
		{
			if (value == null)
			{
				return 0U;
			}
			return uint.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000C3E RID: 3134 RVA: 0x000261C4 File Offset: 0x000243C4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return 0U;
			}
			return uint.Parse(value, NumberStyles.Integer, provider);
		}

		// Token: 0x06000C3F RID: 3135 RVA: 0x000261D3 File Offset: 0x000243D3
		[CLSCompliant(false)]
		public static uint ToUInt32(DateTime value)
		{
			return ((IConvertible)value).ToUInt32(null);
		}

		// Token: 0x06000C40 RID: 3136 RVA: 0x000261E1 File Offset: 0x000243E1
		[__DynamicallyInvokable]
		public static long ToInt64(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToInt64(null);
			}
			return 0L;
		}

		// Token: 0x06000C41 RID: 3137 RVA: 0x000261F5 File Offset: 0x000243F5
		[__DynamicallyInvokable]
		public static long ToInt64(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToInt64(provider);
			}
			return 0L;
		}

		// Token: 0x06000C42 RID: 3138 RVA: 0x00026209 File Offset: 0x00024409
		[__DynamicallyInvokable]
		public static long ToInt64(bool value)
		{
			return value ? 1L : 0L;
		}

		// Token: 0x06000C43 RID: 3139 RVA: 0x00026213 File Offset: 0x00024413
		[__DynamicallyInvokable]
		public static long ToInt64(char value)
		{
			return (long)((ulong)value);
		}

		// Token: 0x06000C44 RID: 3140 RVA: 0x00026217 File Offset: 0x00024417
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static long ToInt64(sbyte value)
		{
			return (long)value;
		}

		// Token: 0x06000C45 RID: 3141 RVA: 0x0002621B File Offset: 0x0002441B
		[__DynamicallyInvokable]
		public static long ToInt64(byte value)
		{
			return (long)((ulong)value);
		}

		// Token: 0x06000C46 RID: 3142 RVA: 0x0002621F File Offset: 0x0002441F
		[__DynamicallyInvokable]
		public static long ToInt64(short value)
		{
			return (long)value;
		}

		// Token: 0x06000C47 RID: 3143 RVA: 0x00026223 File Offset: 0x00024423
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static long ToInt64(ushort value)
		{
			return (long)((ulong)value);
		}

		// Token: 0x06000C48 RID: 3144 RVA: 0x00026227 File Offset: 0x00024427
		[__DynamicallyInvokable]
		public static long ToInt64(int value)
		{
			return (long)value;
		}

		// Token: 0x06000C49 RID: 3145 RVA: 0x0002622B File Offset: 0x0002442B
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static long ToInt64(uint value)
		{
			return (long)((ulong)value);
		}

		// Token: 0x06000C4A RID: 3146 RVA: 0x0002622F File Offset: 0x0002442F
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static long ToInt64(ulong value)
		{
			if (value > 9223372036854775807UL)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int64"));
			}
			return (long)value;
		}

		// Token: 0x06000C4B RID: 3147 RVA: 0x0002624E File Offset: 0x0002444E
		[__DynamicallyInvokable]
		public static long ToInt64(long value)
		{
			return value;
		}

		// Token: 0x06000C4C RID: 3148 RVA: 0x00026251 File Offset: 0x00024451
		[__DynamicallyInvokable]
		public static long ToInt64(float value)
		{
			return Convert.ToInt64((double)value);
		}

		// Token: 0x06000C4D RID: 3149 RVA: 0x0002625A File Offset: 0x0002445A
		[__DynamicallyInvokable]
		public static long ToInt64(double value)
		{
			return checked((long)Math.Round(value));
		}

		// Token: 0x06000C4E RID: 3150 RVA: 0x00026263 File Offset: 0x00024463
		[__DynamicallyInvokable]
		public static long ToInt64(decimal value)
		{
			return decimal.ToInt64(decimal.Round(value, 0));
		}

		// Token: 0x06000C4F RID: 3151 RVA: 0x00026271 File Offset: 0x00024471
		[__DynamicallyInvokable]
		public static long ToInt64(string value)
		{
			if (value == null)
			{
				return 0L;
			}
			return long.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000C50 RID: 3152 RVA: 0x00026284 File Offset: 0x00024484
		[__DynamicallyInvokable]
		public static long ToInt64(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return 0L;
			}
			return long.Parse(value, NumberStyles.Integer, provider);
		}

		// Token: 0x06000C51 RID: 3153 RVA: 0x00026294 File Offset: 0x00024494
		public static long ToInt64(DateTime value)
		{
			return ((IConvertible)value).ToInt64(null);
		}

		// Token: 0x06000C52 RID: 3154 RVA: 0x000262A2 File Offset: 0x000244A2
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToUInt64(null);
			}
			return 0UL;
		}

		// Token: 0x06000C53 RID: 3155 RVA: 0x000262B6 File Offset: 0x000244B6
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToUInt64(provider);
			}
			return 0UL;
		}

		// Token: 0x06000C54 RID: 3156 RVA: 0x000262CA File Offset: 0x000244CA
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(bool value)
		{
			if (!value)
			{
				return 0UL;
			}
			return 1UL;
		}

		// Token: 0x06000C55 RID: 3157 RVA: 0x000262D4 File Offset: 0x000244D4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(char value)
		{
			return (ulong)value;
		}

		// Token: 0x06000C56 RID: 3158 RVA: 0x000262D8 File Offset: 0x000244D8
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(sbyte value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
			}
			return (ulong)((long)value);
		}

		// Token: 0x06000C57 RID: 3159 RVA: 0x000262F0 File Offset: 0x000244F0
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(byte value)
		{
			return (ulong)value;
		}

		// Token: 0x06000C58 RID: 3160 RVA: 0x000262F4 File Offset: 0x000244F4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(short value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
			}
			return (ulong)((long)value);
		}

		// Token: 0x06000C59 RID: 3161 RVA: 0x0002630C File Offset: 0x0002450C
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(ushort value)
		{
			return (ulong)value;
		}

		// Token: 0x06000C5A RID: 3162 RVA: 0x00026310 File Offset: 0x00024510
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(int value)
		{
			if (value < 0)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
			}
			return (ulong)((long)value);
		}

		// Token: 0x06000C5B RID: 3163 RVA: 0x00026328 File Offset: 0x00024528
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(uint value)
		{
			return (ulong)value;
		}

		// Token: 0x06000C5C RID: 3164 RVA: 0x0002632C File Offset: 0x0002452C
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(long value)
		{
			if (value < 0L)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
			}
			return (ulong)value;
		}

		// Token: 0x06000C5D RID: 3165 RVA: 0x00026344 File Offset: 0x00024544
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(ulong value)
		{
			return value;
		}

		// Token: 0x06000C5E RID: 3166 RVA: 0x00026347 File Offset: 0x00024547
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(float value)
		{
			return Convert.ToUInt64((double)value);
		}

		// Token: 0x06000C5F RID: 3167 RVA: 0x00026350 File Offset: 0x00024550
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(double value)
		{
			return checked((ulong)Math.Round(value));
		}

		// Token: 0x06000C60 RID: 3168 RVA: 0x00026359 File Offset: 0x00024559
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(decimal value)
		{
			return decimal.ToUInt64(decimal.Round(value, 0));
		}

		// Token: 0x06000C61 RID: 3169 RVA: 0x00026367 File Offset: 0x00024567
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(string value)
		{
			if (value == null)
			{
				return 0UL;
			}
			return ulong.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000C62 RID: 3170 RVA: 0x0002637A File Offset: 0x0002457A
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return 0UL;
			}
			return ulong.Parse(value, NumberStyles.Integer, provider);
		}

		// Token: 0x06000C63 RID: 3171 RVA: 0x0002638A File Offset: 0x0002458A
		[CLSCompliant(false)]
		public static ulong ToUInt64(DateTime value)
		{
			return ((IConvertible)value).ToUInt64(null);
		}

		// Token: 0x06000C64 RID: 3172 RVA: 0x00026398 File Offset: 0x00024598
		[__DynamicallyInvokable]
		public static float ToSingle(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToSingle(null);
			}
			return 0f;
		}

		// Token: 0x06000C65 RID: 3173 RVA: 0x000263AF File Offset: 0x000245AF
		[__DynamicallyInvokable]
		public static float ToSingle(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToSingle(provider);
			}
			return 0f;
		}

		// Token: 0x06000C66 RID: 3174 RVA: 0x000263C6 File Offset: 0x000245C6
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static float ToSingle(sbyte value)
		{
			return (float)value;
		}

		// Token: 0x06000C67 RID: 3175 RVA: 0x000263CA File Offset: 0x000245CA
		[__DynamicallyInvokable]
		public static float ToSingle(byte value)
		{
			return (float)value;
		}

		// Token: 0x06000C68 RID: 3176 RVA: 0x000263CE File Offset: 0x000245CE
		public static float ToSingle(char value)
		{
			return ((IConvertible)value).ToSingle(null);
		}

		// Token: 0x06000C69 RID: 3177 RVA: 0x000263DC File Offset: 0x000245DC
		[__DynamicallyInvokable]
		public static float ToSingle(short value)
		{
			return (float)value;
		}

		// Token: 0x06000C6A RID: 3178 RVA: 0x000263E0 File Offset: 0x000245E0
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static float ToSingle(ushort value)
		{
			return (float)value;
		}

		// Token: 0x06000C6B RID: 3179 RVA: 0x000263E4 File Offset: 0x000245E4
		[__DynamicallyInvokable]
		public static float ToSingle(int value)
		{
			return (float)value;
		}

		// Token: 0x06000C6C RID: 3180 RVA: 0x000263E8 File Offset: 0x000245E8
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static float ToSingle(uint value)
		{
			return value;
		}

		// Token: 0x06000C6D RID: 3181 RVA: 0x000263ED File Offset: 0x000245ED
		[__DynamicallyInvokable]
		public static float ToSingle(long value)
		{
			return (float)value;
		}

		// Token: 0x06000C6E RID: 3182 RVA: 0x000263F1 File Offset: 0x000245F1
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static float ToSingle(ulong value)
		{
			return value;
		}

		// Token: 0x06000C6F RID: 3183 RVA: 0x000263F6 File Offset: 0x000245F6
		[__DynamicallyInvokable]
		public static float ToSingle(float value)
		{
			return value;
		}

		// Token: 0x06000C70 RID: 3184 RVA: 0x000263F9 File Offset: 0x000245F9
		[__DynamicallyInvokable]
		public static float ToSingle(double value)
		{
			return (float)value;
		}

		// Token: 0x06000C71 RID: 3185 RVA: 0x000263FD File Offset: 0x000245FD
		[__DynamicallyInvokable]
		public static float ToSingle(decimal value)
		{
			return (float)value;
		}

		// Token: 0x06000C72 RID: 3186 RVA: 0x00026406 File Offset: 0x00024606
		[__DynamicallyInvokable]
		public static float ToSingle(string value)
		{
			if (value == null)
			{
				return 0f;
			}
			return float.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000C73 RID: 3187 RVA: 0x0002641C File Offset: 0x0002461C
		[__DynamicallyInvokable]
		public static float ToSingle(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return 0f;
			}
			return float.Parse(value, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, provider);
		}

		// Token: 0x06000C74 RID: 3188 RVA: 0x00026433 File Offset: 0x00024633
		[__DynamicallyInvokable]
		public static float ToSingle(bool value)
		{
			return (float)(value ? 1 : 0);
		}

		// Token: 0x06000C75 RID: 3189 RVA: 0x0002643D File Offset: 0x0002463D
		public static float ToSingle(DateTime value)
		{
			return ((IConvertible)value).ToSingle(null);
		}

		// Token: 0x06000C76 RID: 3190 RVA: 0x0002644B File Offset: 0x0002464B
		[__DynamicallyInvokable]
		public static double ToDouble(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToDouble(null);
			}
			return 0.0;
		}

		// Token: 0x06000C77 RID: 3191 RVA: 0x00026466 File Offset: 0x00024666
		[__DynamicallyInvokable]
		public static double ToDouble(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToDouble(provider);
			}
			return 0.0;
		}

		// Token: 0x06000C78 RID: 3192 RVA: 0x00026481 File Offset: 0x00024681
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static double ToDouble(sbyte value)
		{
			return (double)value;
		}

		// Token: 0x06000C79 RID: 3193 RVA: 0x00026485 File Offset: 0x00024685
		[__DynamicallyInvokable]
		public static double ToDouble(byte value)
		{
			return (double)value;
		}

		// Token: 0x06000C7A RID: 3194 RVA: 0x00026489 File Offset: 0x00024689
		[__DynamicallyInvokable]
		public static double ToDouble(short value)
		{
			return (double)value;
		}

		// Token: 0x06000C7B RID: 3195 RVA: 0x0002648D File Offset: 0x0002468D
		public static double ToDouble(char value)
		{
			return ((IConvertible)value).ToDouble(null);
		}

		// Token: 0x06000C7C RID: 3196 RVA: 0x0002649B File Offset: 0x0002469B
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static double ToDouble(ushort value)
		{
			return (double)value;
		}

		// Token: 0x06000C7D RID: 3197 RVA: 0x0002649F File Offset: 0x0002469F
		[__DynamicallyInvokable]
		public static double ToDouble(int value)
		{
			return (double)value;
		}

		// Token: 0x06000C7E RID: 3198 RVA: 0x000264A3 File Offset: 0x000246A3
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static double ToDouble(uint value)
		{
			return value;
		}

		// Token: 0x06000C7F RID: 3199 RVA: 0x000264A8 File Offset: 0x000246A8
		[__DynamicallyInvokable]
		public static double ToDouble(long value)
		{
			return (double)value;
		}

		// Token: 0x06000C80 RID: 3200 RVA: 0x000264AC File Offset: 0x000246AC
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static double ToDouble(ulong value)
		{
			return value;
		}

		// Token: 0x06000C81 RID: 3201 RVA: 0x000264B1 File Offset: 0x000246B1
		[__DynamicallyInvokable]
		public static double ToDouble(float value)
		{
			return (double)value;
		}

		// Token: 0x06000C82 RID: 3202 RVA: 0x000264B5 File Offset: 0x000246B5
		[__DynamicallyInvokable]
		public static double ToDouble(double value)
		{
			return value;
		}

		// Token: 0x06000C83 RID: 3203 RVA: 0x000264B8 File Offset: 0x000246B8
		[__DynamicallyInvokable]
		public static double ToDouble(decimal value)
		{
			return (double)value;
		}

		// Token: 0x06000C84 RID: 3204 RVA: 0x000264C1 File Offset: 0x000246C1
		[__DynamicallyInvokable]
		public static double ToDouble(string value)
		{
			if (value == null)
			{
				return 0.0;
			}
			return double.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000C85 RID: 3205 RVA: 0x000264DB File Offset: 0x000246DB
		[__DynamicallyInvokable]
		public static double ToDouble(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return 0.0;
			}
			return double.Parse(value, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, provider);
		}

		// Token: 0x06000C86 RID: 3206 RVA: 0x000264F6 File Offset: 0x000246F6
		[__DynamicallyInvokable]
		public static double ToDouble(bool value)
		{
			return (double)(value ? 1 : 0);
		}

		// Token: 0x06000C87 RID: 3207 RVA: 0x00026500 File Offset: 0x00024700
		public static double ToDouble(DateTime value)
		{
			return ((IConvertible)value).ToDouble(null);
		}

		// Token: 0x06000C88 RID: 3208 RVA: 0x0002650E File Offset: 0x0002470E
		[__DynamicallyInvokable]
		public static decimal ToDecimal(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToDecimal(null);
			}
			return 0m;
		}

		// Token: 0x06000C89 RID: 3209 RVA: 0x00026525 File Offset: 0x00024725
		[__DynamicallyInvokable]
		public static decimal ToDecimal(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToDecimal(provider);
			}
			return 0m;
		}

		// Token: 0x06000C8A RID: 3210 RVA: 0x0002653C File Offset: 0x0002473C
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static decimal ToDecimal(sbyte value)
		{
			return value;
		}

		// Token: 0x06000C8B RID: 3211 RVA: 0x00026544 File Offset: 0x00024744
		[__DynamicallyInvokable]
		public static decimal ToDecimal(byte value)
		{
			return value;
		}

		// Token: 0x06000C8C RID: 3212 RVA: 0x0002654C File Offset: 0x0002474C
		public static decimal ToDecimal(char value)
		{
			return ((IConvertible)value).ToDecimal(null);
		}

		// Token: 0x06000C8D RID: 3213 RVA: 0x0002655A File Offset: 0x0002475A
		[__DynamicallyInvokable]
		public static decimal ToDecimal(short value)
		{
			return value;
		}

		// Token: 0x06000C8E RID: 3214 RVA: 0x00026562 File Offset: 0x00024762
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static decimal ToDecimal(ushort value)
		{
			return value;
		}

		// Token: 0x06000C8F RID: 3215 RVA: 0x0002656A File Offset: 0x0002476A
		[__DynamicallyInvokable]
		public static decimal ToDecimal(int value)
		{
			return value;
		}

		// Token: 0x06000C90 RID: 3216 RVA: 0x00026572 File Offset: 0x00024772
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static decimal ToDecimal(uint value)
		{
			return value;
		}

		// Token: 0x06000C91 RID: 3217 RVA: 0x0002657A File Offset: 0x0002477A
		[__DynamicallyInvokable]
		public static decimal ToDecimal(long value)
		{
			return value;
		}

		// Token: 0x06000C92 RID: 3218 RVA: 0x00026582 File Offset: 0x00024782
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static decimal ToDecimal(ulong value)
		{
			return value;
		}

		// Token: 0x06000C93 RID: 3219 RVA: 0x0002658A File Offset: 0x0002478A
		[__DynamicallyInvokable]
		public static decimal ToDecimal(float value)
		{
			return (decimal)value;
		}

		// Token: 0x06000C94 RID: 3220 RVA: 0x00026592 File Offset: 0x00024792
		[__DynamicallyInvokable]
		public static decimal ToDecimal(double value)
		{
			return (decimal)value;
		}

		// Token: 0x06000C95 RID: 3221 RVA: 0x0002659A File Offset: 0x0002479A
		[__DynamicallyInvokable]
		public static decimal ToDecimal(string value)
		{
			if (value == null)
			{
				return 0m;
			}
			return decimal.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000C96 RID: 3222 RVA: 0x000265B0 File Offset: 0x000247B0
		[__DynamicallyInvokable]
		public static decimal ToDecimal(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return 0m;
			}
			return decimal.Parse(value, NumberStyles.Number, provider);
		}

		// Token: 0x06000C97 RID: 3223 RVA: 0x000265C4 File Offset: 0x000247C4
		[__DynamicallyInvokable]
		public static decimal ToDecimal(decimal value)
		{
			return value;
		}

		// Token: 0x06000C98 RID: 3224 RVA: 0x000265C7 File Offset: 0x000247C7
		[__DynamicallyInvokable]
		public static decimal ToDecimal(bool value)
		{
			return value ? 1 : 0;
		}

		// Token: 0x06000C99 RID: 3225 RVA: 0x000265D5 File Offset: 0x000247D5
		public static decimal ToDecimal(DateTime value)
		{
			return ((IConvertible)value).ToDecimal(null);
		}

		// Token: 0x06000C9A RID: 3226 RVA: 0x000265E3 File Offset: 0x000247E3
		public static DateTime ToDateTime(DateTime value)
		{
			return value;
		}

		// Token: 0x06000C9B RID: 3227 RVA: 0x000265E6 File Offset: 0x000247E6
		[__DynamicallyInvokable]
		public static DateTime ToDateTime(object value)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToDateTime(null);
			}
			return DateTime.MinValue;
		}

		// Token: 0x06000C9C RID: 3228 RVA: 0x000265FD File Offset: 0x000247FD
		[__DynamicallyInvokable]
		public static DateTime ToDateTime(object value, IFormatProvider provider)
		{
			if (value != null)
			{
				return ((IConvertible)value).ToDateTime(provider);
			}
			return DateTime.MinValue;
		}

		// Token: 0x06000C9D RID: 3229 RVA: 0x00026614 File Offset: 0x00024814
		[__DynamicallyInvokable]
		public static DateTime ToDateTime(string value)
		{
			if (value == null)
			{
				return new DateTime(0L);
			}
			return DateTime.Parse(value, CultureInfo.CurrentCulture);
		}

		// Token: 0x06000C9E RID: 3230 RVA: 0x0002662C File Offset: 0x0002482C
		[__DynamicallyInvokable]
		public static DateTime ToDateTime(string value, IFormatProvider provider)
		{
			if (value == null)
			{
				return new DateTime(0L);
			}
			return DateTime.Parse(value, provider);
		}

		// Token: 0x06000C9F RID: 3231 RVA: 0x00026640 File Offset: 0x00024840
		[CLSCompliant(false)]
		public static DateTime ToDateTime(sbyte value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CA0 RID: 3232 RVA: 0x0002664E File Offset: 0x0002484E
		public static DateTime ToDateTime(byte value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CA1 RID: 3233 RVA: 0x0002665C File Offset: 0x0002485C
		public static DateTime ToDateTime(short value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CA2 RID: 3234 RVA: 0x0002666A File Offset: 0x0002486A
		[CLSCompliant(false)]
		public static DateTime ToDateTime(ushort value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CA3 RID: 3235 RVA: 0x00026678 File Offset: 0x00024878
		public static DateTime ToDateTime(int value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CA4 RID: 3236 RVA: 0x00026686 File Offset: 0x00024886
		[CLSCompliant(false)]
		public static DateTime ToDateTime(uint value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CA5 RID: 3237 RVA: 0x00026694 File Offset: 0x00024894
		public static DateTime ToDateTime(long value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CA6 RID: 3238 RVA: 0x000266A2 File Offset: 0x000248A2
		[CLSCompliant(false)]
		public static DateTime ToDateTime(ulong value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CA7 RID: 3239 RVA: 0x000266B0 File Offset: 0x000248B0
		public static DateTime ToDateTime(bool value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CA8 RID: 3240 RVA: 0x000266BE File Offset: 0x000248BE
		public static DateTime ToDateTime(char value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CA9 RID: 3241 RVA: 0x000266CC File Offset: 0x000248CC
		public static DateTime ToDateTime(float value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CAA RID: 3242 RVA: 0x000266DA File Offset: 0x000248DA
		public static DateTime ToDateTime(double value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CAB RID: 3243 RVA: 0x000266E8 File Offset: 0x000248E8
		public static DateTime ToDateTime(decimal value)
		{
			return ((IConvertible)value).ToDateTime(null);
		}

		// Token: 0x06000CAC RID: 3244 RVA: 0x000266F6 File Offset: 0x000248F6
		[__DynamicallyInvokable]
		public static string ToString(object value)
		{
			return Convert.ToString(value, null);
		}

		// Token: 0x06000CAD RID: 3245 RVA: 0x00026700 File Offset: 0x00024900
		[__DynamicallyInvokable]
		public static string ToString(object value, IFormatProvider provider)
		{
			IConvertible convertible = value as IConvertible;
			if (convertible != null)
			{
				return convertible.ToString(provider);
			}
			IFormattable formattable = value as IFormattable;
			if (formattable != null)
			{
				return formattable.ToString(null, provider);
			}
			if (value != null)
			{
				return value.ToString();
			}
			return string.Empty;
		}

		// Token: 0x06000CAE RID: 3246 RVA: 0x00026741 File Offset: 0x00024941
		[__DynamicallyInvokable]
		public static string ToString(bool value)
		{
			return value.ToString();
		}

		// Token: 0x06000CAF RID: 3247 RVA: 0x0002674A File Offset: 0x0002494A
		[__DynamicallyInvokable]
		public static string ToString(bool value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CB0 RID: 3248 RVA: 0x00026754 File Offset: 0x00024954
		[__DynamicallyInvokable]
		public static string ToString(char value)
		{
			return char.ToString(value);
		}

		// Token: 0x06000CB1 RID: 3249 RVA: 0x0002675C File Offset: 0x0002495C
		[__DynamicallyInvokable]
		public static string ToString(char value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CB2 RID: 3250 RVA: 0x00026766 File Offset: 0x00024966
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static string ToString(sbyte value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CB3 RID: 3251 RVA: 0x00026774 File Offset: 0x00024974
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static string ToString(sbyte value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CB4 RID: 3252 RVA: 0x0002677E File Offset: 0x0002497E
		[__DynamicallyInvokable]
		public static string ToString(byte value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CB5 RID: 3253 RVA: 0x0002678C File Offset: 0x0002498C
		[__DynamicallyInvokable]
		public static string ToString(byte value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CB6 RID: 3254 RVA: 0x00026796 File Offset: 0x00024996
		[__DynamicallyInvokable]
		public static string ToString(short value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CB7 RID: 3255 RVA: 0x000267A4 File Offset: 0x000249A4
		[__DynamicallyInvokable]
		public static string ToString(short value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CB8 RID: 3256 RVA: 0x000267AE File Offset: 0x000249AE
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static string ToString(ushort value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CB9 RID: 3257 RVA: 0x000267BC File Offset: 0x000249BC
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static string ToString(ushort value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CBA RID: 3258 RVA: 0x000267C6 File Offset: 0x000249C6
		[__DynamicallyInvokable]
		public static string ToString(int value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CBB RID: 3259 RVA: 0x000267D4 File Offset: 0x000249D4
		[__DynamicallyInvokable]
		public static string ToString(int value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CBC RID: 3260 RVA: 0x000267DE File Offset: 0x000249DE
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static string ToString(uint value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CBD RID: 3261 RVA: 0x000267EC File Offset: 0x000249EC
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static string ToString(uint value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CBE RID: 3262 RVA: 0x000267F6 File Offset: 0x000249F6
		[__DynamicallyInvokable]
		public static string ToString(long value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CBF RID: 3263 RVA: 0x00026804 File Offset: 0x00024A04
		[__DynamicallyInvokable]
		public static string ToString(long value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CC0 RID: 3264 RVA: 0x0002680E File Offset: 0x00024A0E
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static string ToString(ulong value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CC1 RID: 3265 RVA: 0x0002681C File Offset: 0x00024A1C
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static string ToString(ulong value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CC2 RID: 3266 RVA: 0x00026826 File Offset: 0x00024A26
		[__DynamicallyInvokable]
		public static string ToString(float value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CC3 RID: 3267 RVA: 0x00026834 File Offset: 0x00024A34
		[__DynamicallyInvokable]
		public static string ToString(float value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CC4 RID: 3268 RVA: 0x0002683E File Offset: 0x00024A3E
		[__DynamicallyInvokable]
		public static string ToString(double value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CC5 RID: 3269 RVA: 0x0002684C File Offset: 0x00024A4C
		[__DynamicallyInvokable]
		public static string ToString(double value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CC6 RID: 3270 RVA: 0x00026856 File Offset: 0x00024A56
		[__DynamicallyInvokable]
		public static string ToString(decimal value)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}

		// Token: 0x06000CC7 RID: 3271 RVA: 0x00026864 File Offset: 0x00024A64
		[__DynamicallyInvokable]
		public static string ToString(decimal value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CC8 RID: 3272 RVA: 0x0002686E File Offset: 0x00024A6E
		[__DynamicallyInvokable]
		public static string ToString(DateTime value)
		{
			return value.ToString();
		}

		// Token: 0x06000CC9 RID: 3273 RVA: 0x00026877 File Offset: 0x00024A77
		[__DynamicallyInvokable]
		public static string ToString(DateTime value, IFormatProvider provider)
		{
			return value.ToString(provider);
		}

		// Token: 0x06000CCA RID: 3274 RVA: 0x00026881 File Offset: 0x00024A81
		public static string ToString(string value)
		{
			return value;
		}

		// Token: 0x06000CCB RID: 3275 RVA: 0x00026884 File Offset: 0x00024A84
		public static string ToString(string value, IFormatProvider provider)
		{
			return value;
		}

		// Token: 0x06000CCC RID: 3276 RVA: 0x00026888 File Offset: 0x00024A88
		[__DynamicallyInvokable]
		public static byte ToByte(string value, int fromBase)
		{
			if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			int num = ParseNumbers.StringToInt(value, fromBase, 4608);
			if (num < 0 || num > 255)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
			}
			return (byte)num;
		}

		// Token: 0x06000CCD RID: 3277 RVA: 0x000268E4 File Offset: 0x00024AE4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte ToSByte(string value, int fromBase)
		{
			if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			int num = ParseNumbers.StringToInt(value, fromBase, 5120);
			if (fromBase != 10 && num <= 255)
			{
				return (sbyte)num;
			}
			if (num < -128 || num > 127)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
			}
			return (sbyte)num;
		}

		// Token: 0x06000CCE RID: 3278 RVA: 0x0002694C File Offset: 0x00024B4C
		[__DynamicallyInvokable]
		public static short ToInt16(string value, int fromBase)
		{
			if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			int num = ParseNumbers.StringToInt(value, fromBase, 6144);
			if (fromBase != 10 && num <= 65535)
			{
				return (short)num;
			}
			if (num < -32768 || num > 32767)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
			}
			return (short)num;
		}

		// Token: 0x06000CCF RID: 3279 RVA: 0x000269BC File Offset: 0x00024BBC
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(string value, int fromBase)
		{
			if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			int num = ParseNumbers.StringToInt(value, fromBase, 4608);
			if (num < 0 || num > 65535)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
			}
			return (ushort)num;
		}

		// Token: 0x06000CD0 RID: 3280 RVA: 0x00026A16 File Offset: 0x00024C16
		[__DynamicallyInvokable]
		public static int ToInt32(string value, int fromBase)
		{
			if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			return ParseNumbers.StringToInt(value, fromBase, 4096);
		}

		// Token: 0x06000CD1 RID: 3281 RVA: 0x00026A46 File Offset: 0x00024C46
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(string value, int fromBase)
		{
			if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			return (uint)ParseNumbers.StringToInt(value, fromBase, 4608);
		}

		// Token: 0x06000CD2 RID: 3282 RVA: 0x00026A76 File Offset: 0x00024C76
		[__DynamicallyInvokable]
		public static long ToInt64(string value, int fromBase)
		{
			if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			return ParseNumbers.StringToLong(value, fromBase, 4096);
		}

		// Token: 0x06000CD3 RID: 3283 RVA: 0x00026AA6 File Offset: 0x00024CA6
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(string value, int fromBase)
		{
			if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			return (ulong)ParseNumbers.StringToLong(value, fromBase, 4608);
		}

		// Token: 0x06000CD4 RID: 3284 RVA: 0x00026AD6 File Offset: 0x00024CD6
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static string ToString(byte value, int toBase)
		{
			if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			return ParseNumbers.IntToString((int)value, toBase, -1, ' ', 64);
		}

		// Token: 0x06000CD5 RID: 3285 RVA: 0x00026B06 File Offset: 0x00024D06
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static string ToString(short value, int toBase)
		{
			if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			return ParseNumbers.IntToString((int)value, toBase, -1, ' ', 128);
		}

		// Token: 0x06000CD6 RID: 3286 RVA: 0x00026B39 File Offset: 0x00024D39
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static string ToString(int value, int toBase)
		{
			if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			return ParseNumbers.IntToString(value, toBase, -1, ' ', 0);
		}

		// Token: 0x06000CD7 RID: 3287 RVA: 0x00026B68 File Offset: 0x00024D68
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static string ToString(long value, int toBase)
		{
			if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
			}
			return ParseNumbers.LongToString(value, toBase, -1, ' ', 0);
		}

		// Token: 0x06000CD8 RID: 3288 RVA: 0x00026B97 File Offset: 0x00024D97
		[__DynamicallyInvokable]
		public static string ToBase64String(byte[] inArray)
		{
			if (inArray == null)
			{
				throw new ArgumentNullException("inArray");
			}
			return Convert.ToBase64String(inArray, 0, inArray.Length, Base64FormattingOptions.None);
		}

		// Token: 0x06000CD9 RID: 3289 RVA: 0x00026BB2 File Offset: 0x00024DB2
		[ComVisible(false)]
		public static string ToBase64String(byte[] inArray, Base64FormattingOptions options)
		{
			if (inArray == null)
			{
				throw new ArgumentNullException("inArray");
			}
			return Convert.ToBase64String(inArray, 0, inArray.Length, options);
		}

		// Token: 0x06000CDA RID: 3290 RVA: 0x00026BCD File Offset: 0x00024DCD
		[__DynamicallyInvokable]
		public static string ToBase64String(byte[] inArray, int offset, int length)
		{
			return Convert.ToBase64String(inArray, offset, length, Base64FormattingOptions.None);
		}

		// Token: 0x06000CDB RID: 3291 RVA: 0x00026BD8 File Offset: 0x00024DD8
		[SecuritySafeCritical]
		[ComVisible(false)]
		public unsafe static string ToBase64String(byte[] inArray, int offset, int length, Base64FormattingOptions options)
		{
			if (inArray == null)
			{
				throw new ArgumentNullException("inArray");
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (options < Base64FormattingOptions.None || options > Base64FormattingOptions.InsertLineBreaks)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)options }));
			}
			int num = inArray.Length;
			if (offset > num - length)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
			}
			if (num == 0)
			{
				return string.Empty;
			}
			bool insertLineBreaks = options == Base64FormattingOptions.InsertLineBreaks;
			int length2 = Convert.ToBase64_CalculateAndValidateOutputLength(length, insertLineBreaks);
			string text = string.FastAllocateString(length2);
			fixed (string text2 = text)
			{
				char* ptr = text2;
				if (ptr != null)
				{
					ptr += RuntimeHelpers.OffsetToStringData / 2;
				}
				byte* inData;
				if (inArray == null || inArray.Length == 0)
				{
					inData = null;
				}
				else
				{
					inData = &inArray[0];
				}
				int num2 = Convert.ConvertToBase64Array(ptr, inData, offset, length, insertLineBreaks);
				return text;
			}
		}

		// Token: 0x06000CDC RID: 3292 RVA: 0x00026CCA File Offset: 0x00024ECA
		[__DynamicallyInvokable]
		public static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut)
		{
			return Convert.ToBase64CharArray(inArray, offsetIn, length, outArray, offsetOut, Base64FormattingOptions.None);
		}

		// Token: 0x06000CDD RID: 3293 RVA: 0x00026CD8 File Offset: 0x00024ED8
		[SecuritySafeCritical]
		[ComVisible(false)]
		public unsafe static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut, Base64FormattingOptions options)
		{
			if (inArray == null)
			{
				throw new ArgumentNullException("inArray");
			}
			if (outArray == null)
			{
				throw new ArgumentNullException("outArray");
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (offsetIn < 0)
			{
				throw new ArgumentOutOfRangeException("offsetIn", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (offsetOut < 0)
			{
				throw new ArgumentOutOfRangeException("offsetOut", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (options < Base64FormattingOptions.None || options > Base64FormattingOptions.InsertLineBreaks)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)options }));
			}
			int num = inArray.Length;
			if (offsetIn > num - length)
			{
				throw new ArgumentOutOfRangeException("offsetIn", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
			}
			if (num == 0)
			{
				return 0;
			}
			bool insertLineBreaks = options == Base64FormattingOptions.InsertLineBreaks;
			int num2 = outArray.Length;
			int num3 = Convert.ToBase64_CalculateAndValidateOutputLength(length, insertLineBreaks);
			if (offsetOut > num2 - num3)
			{
				throw new ArgumentOutOfRangeException("offsetOut", Environment.GetResourceString("ArgumentOutOfRange_OffsetOut"));
			}
			int result;
			fixed (char* ptr = &outArray[offsetOut])
			{
				char* outChars = ptr;
				fixed (byte[] array = inArray)
				{
					byte* inData;
					if (inArray == null || array.Length == 0)
					{
						inData = null;
					}
					else
					{
						inData = &array[0];
					}
					result = Convert.ConvertToBase64Array(outChars, inData, offsetIn, length, insertLineBreaks);
				}
			}
			return result;
		}

		// Token: 0x06000CDE RID: 3294 RVA: 0x00026E10 File Offset: 0x00025010
		[SecurityCritical]
		private unsafe static int ConvertToBase64Array(char* outChars, byte* inData, int offset, int length, bool insertLineBreaks)
		{
			int num = length % 3;
			int num2 = offset + (length - num);
			int num3 = 0;
			int num4 = 0;
			char[] array;
			char* ptr;
			if ((array = Convert.base64Table) == null || array.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array[0];
			}
			int i;
			for (i = offset; i < num2; i += 3)
			{
				if (insertLineBreaks)
				{
					if (num4 == 76)
					{
						outChars[num3++] = '\r';
						outChars[num3++] = '\n';
						num4 = 0;
					}
					num4 += 4;
				}
				outChars[num3] = ptr[(inData[i] & 252) >> 2];
				outChars[num3 + 1] = ptr[((int)(inData[i] & 3) << 4) | ((inData[i + 1] & 240) >> 4)];
				outChars[num3 + 2] = ptr[((int)(inData[i + 1] & 15) << 2) | ((inData[i + 2] & 192) >> 6)];
				outChars[num3 + 3] = ptr[inData[i + 2] & 63];
				num3 += 4;
			}
			i = num2;
			if (insertLineBreaks && num != 0 && num4 == 76)
			{
				outChars[num3++] = '\r';
				outChars[num3++] = '\n';
			}
			if (num != 1)
			{
				if (num == 2)
				{
					outChars[num3] = ptr[(inData[i] & 252) >> 2];
					outChars[num3 + 1] = ptr[((int)(inData[i] & 3) << 4) | ((inData[i + 1] & 240) >> 4)];
					outChars[num3 + 2] = ptr[(inData[i + 1] & 15) << 2];
					outChars[num3 + 3] = ptr[64];
					num3 += 4;
				}
			}
			else
			{
				outChars[num3] = ptr[(inData[i] & 252) >> 2];
				outChars[num3 + 1] = ptr[(inData[i] & 3) << 4];
				outChars[num3 + 2] = ptr[64];
				outChars[num3 + 3] = ptr[64];
				num3 += 4;
			}
			array = null;
			return num3;
		}

		// Token: 0x06000CDF RID: 3295 RVA: 0x00027028 File Offset: 0x00025228
		private static int ToBase64_CalculateAndValidateOutputLength(int inputLength, bool insertLineBreaks)
		{
			long num = (long)inputLength / 3L * 4L;
			num += ((inputLength % 3 != 0) ? 4L : 0L);
			if (num == 0L)
			{
				return 0;
			}
			if (insertLineBreaks)
			{
				long num2 = num / 76L;
				if (num % 76L == 0L)
				{
					num2 -= 1L;
				}
				num += num2 * 2L;
			}
			if (num > 2147483647L)
			{
				throw new OutOfMemoryException();
			}
			return (int)num;
		}

		// Token: 0x06000CE0 RID: 3296 RVA: 0x00027080 File Offset: 0x00025280
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static byte[] FromBase64String(string s)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			char* ptr = s;
			if (ptr != null)
			{
				ptr += RuntimeHelpers.OffsetToStringData / 2;
			}
			return Convert.FromBase64CharPtr(ptr, s.Length);
		}

		// Token: 0x06000CE1 RID: 3297 RVA: 0x000270B8 File Offset: 0x000252B8
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static byte[] FromBase64CharArray(char[] inArray, int offset, int length)
		{
			if (inArray == null)
			{
				throw new ArgumentNullException("inArray");
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (offset > inArray.Length - length)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
			}
			char* ptr;
			if (inArray == null || inArray.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &inArray[0];
			}
			return Convert.FromBase64CharPtr(ptr + offset, length);
		}

		// Token: 0x06000CE2 RID: 3298 RVA: 0x00027148 File Offset: 0x00025348
		[SecurityCritical]
		private unsafe static byte[] FromBase64CharPtr(char* inputPtr, int inputLength)
		{
			while (inputLength > 0)
			{
				int num = (int)inputPtr[inputLength - 1];
				if (num != 32 && num != 10 && num != 13 && num != 9)
				{
					break;
				}
				inputLength--;
			}
			int num2 = Convert.FromBase64_ComputeResultLength(inputPtr, inputLength);
			byte[] array = new byte[num2];
			byte[] array2;
			byte* startDestPtr;
			if ((array2 = array) == null || array2.Length == 0)
			{
				startDestPtr = null;
			}
			else
			{
				startDestPtr = &array2[0];
			}
			int num3 = Convert.FromBase64_Decode(inputPtr, inputLength, startDestPtr, num2);
			array2 = null;
			return array;
		}

		// Token: 0x06000CE3 RID: 3299 RVA: 0x000271BC File Offset: 0x000253BC
		[SecurityCritical]
		private unsafe static int FromBase64_Decode(char* startInputPtr, int inputLength, byte* startDestPtr, int destLength)
		{
			char* ptr = startInputPtr;
			byte* ptr2 = startDestPtr;
			char* ptr3 = ptr + inputLength;
			byte* ptr4 = ptr2 + destLength;
			uint num = 255U;
			while (ptr < ptr3)
			{
				uint num2 = (uint)(*ptr);
				ptr++;
				if (num2 - 65U <= 25U)
				{
					num2 -= 65U;
				}
				else if (num2 - 97U <= 25U)
				{
					num2 -= 71U;
				}
				else
				{
					if (num2 - 48U > 9U)
					{
						if (num2 <= 32U)
						{
							if (num2 - 9U <= 1U || num2 == 13U || num2 == 32U)
							{
								continue;
							}
						}
						else
						{
							if (num2 == 43U)
							{
								num2 = 62U;
								goto IL_A7;
							}
							if (num2 == 47U)
							{
								num2 = 63U;
								goto IL_A7;
							}
							if (num2 == 61U)
							{
								if (ptr == ptr3)
								{
									num <<= 6;
									if ((num & 2147483648U) == 0U)
									{
										throw new FormatException(Environment.GetResourceString("Format_BadBase64CharArrayLength"));
									}
									if ((int)((long)(ptr4 - ptr2)) < 2)
									{
										return -1;
									}
									*(ptr2++) = (byte)(num >> 16);
									*(ptr2++) = (byte)(num >> 8);
									num = 255U;
									break;
								}
								else
								{
									while (ptr < ptr3 - 1)
									{
										int num3 = (int)(*ptr);
										if (num3 != 32 && num3 != 10 && num3 != 13 && num3 != 9)
										{
											break;
										}
										ptr++;
									}
									if (ptr != ptr3 - 1 || *ptr != '=')
									{
										throw new FormatException(Environment.GetResourceString("Format_BadBase64Char"));
									}
									num <<= 12;
									if ((num & 2147483648U) == 0U)
									{
										throw new FormatException(Environment.GetResourceString("Format_BadBase64CharArrayLength"));
									}
									if ((int)((long)(ptr4 - ptr2)) < 1)
									{
										return -1;
									}
									*(ptr2++) = (byte)(num >> 16);
									num = 255U;
									break;
								}
							}
						}
						throw new FormatException(Environment.GetResourceString("Format_BadBase64Char"));
					}
					num2 -= 4294967292U;
				}
				IL_A7:
				num = (num << 6) | num2;
				if ((num & 2147483648U) != 0U)
				{
					if ((int)((long)(ptr4 - ptr2)) < 3)
					{
						return -1;
					}
					*ptr2 = (byte)(num >> 16);
					ptr2[1] = (byte)(num >> 8);
					ptr2[2] = (byte)num;
					ptr2 += 3;
					num = 255U;
				}
			}
			if (num != 255U)
			{
				throw new FormatException(Environment.GetResourceString("Format_BadBase64CharArrayLength"));
			}
			return (int)((long)(ptr2 - startDestPtr));
		}

		// Token: 0x06000CE4 RID: 3300 RVA: 0x000273B4 File Offset: 0x000255B4
		[SecurityCritical]
		private unsafe static int FromBase64_ComputeResultLength(char* inputPtr, int inputLength)
		{
			char* ptr = inputPtr + inputLength;
			int num = inputLength;
			int num2 = 0;
			while (inputPtr < ptr)
			{
				uint num3 = (uint)(*inputPtr);
				inputPtr++;
				if (num3 <= 32U)
				{
					num--;
				}
				else if (num3 == 61U)
				{
					num--;
					num2++;
				}
			}
			if (num2 != 0)
			{
				if (num2 == 1)
				{
					num2 = 2;
				}
				else
				{
					if (num2 != 2)
					{
						throw new FormatException(Environment.GetResourceString("Format_BadBase64Char"));
					}
					num2 = 1;
				}
			}
			return num / 4 * 3 + num2;
		}

		// Token: 0x04000534 RID: 1332
		internal static readonly RuntimeType[] ConvertTypes = new RuntimeType[]
		{
			(RuntimeType)typeof(Empty),
			(RuntimeType)typeof(object),
			(RuntimeType)typeof(DBNull),
			(RuntimeType)typeof(bool),
			(RuntimeType)typeof(char),
			(RuntimeType)typeof(sbyte),
			(RuntimeType)typeof(byte),
			(RuntimeType)typeof(short),
			(RuntimeType)typeof(ushort),
			(RuntimeType)typeof(int),
			(RuntimeType)typeof(uint),
			(RuntimeType)typeof(long),
			(RuntimeType)typeof(ulong),
			(RuntimeType)typeof(float),
			(RuntimeType)typeof(double),
			(RuntimeType)typeof(decimal),
			(RuntimeType)typeof(DateTime),
			(RuntimeType)typeof(object),
			(RuntimeType)typeof(string)
		};

		// Token: 0x04000535 RID: 1333
		private static readonly RuntimeType EnumType = (RuntimeType)typeof(Enum);

		// Token: 0x04000536 RID: 1334
		internal static readonly char[] base64Table = new char[]
		{
			'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
			'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
			'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd',
			'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n',
			'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
			'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7',
			'8', '9', '+', '/', '='
		};

		// Token: 0x04000537 RID: 1335
		private const int base64LineBreakPosition = 76;

		// Token: 0x04000538 RID: 1336
		public static readonly object DBNull = System.DBNull.Value;
	}
}
