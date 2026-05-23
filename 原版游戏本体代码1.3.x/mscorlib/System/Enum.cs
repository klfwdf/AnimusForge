using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System
{
	// Token: 0x020000DA RID: 218
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class Enum : ValueType, IComparable, IFormattable, IConvertible
	{
		// Token: 0x06000DEB RID: 3563 RVA: 0x0002A970 File Offset: 0x00028B70
		[SecuritySafeCritical]
		private static Enum.ValuesAndNames GetCachedValuesAndNames(RuntimeType enumType, bool getNames)
		{
			Enum.ValuesAndNames valuesAndNames = enumType.GenericCache as Enum.ValuesAndNames;
			if (valuesAndNames == null || (getNames && valuesAndNames.Names == null))
			{
				ulong[] values = null;
				string[] names = null;
				Enum.GetEnumValuesAndNames(enumType.GetTypeHandleInternal(), JitHelpers.GetObjectHandleOnStack<ulong[]>(ref values), JitHelpers.GetObjectHandleOnStack<string[]>(ref names), getNames);
				valuesAndNames = new Enum.ValuesAndNames(values, names);
				enumType.GenericCache = valuesAndNames;
			}
			return valuesAndNames;
		}

		// Token: 0x06000DEC RID: 3564 RVA: 0x0002A9C8 File Offset: 0x00028BC8
		private static string InternalFormattedHexString(object value)
		{
			switch (Convert.GetTypeCode(value))
			{
			case TypeCode.Boolean:
				return Convert.ToByte((bool)value).ToString("X2", null);
			case TypeCode.Char:
				return ((ushort)((char)value)).ToString("X4", null);
			case TypeCode.SByte:
				return ((byte)((sbyte)value)).ToString("X2", null);
			case TypeCode.Byte:
				return ((byte)value).ToString("X2", null);
			case TypeCode.Int16:
				return ((ushort)((short)value)).ToString("X4", null);
			case TypeCode.UInt16:
				return ((ushort)value).ToString("X4", null);
			case TypeCode.Int32:
				return ((uint)((int)value)).ToString("X8", null);
			case TypeCode.UInt32:
				return ((uint)value).ToString("X8", null);
			case TypeCode.Int64:
				return ((ulong)((long)value)).ToString("X16", null);
			case TypeCode.UInt64:
				return ((ulong)value).ToString("X16", null);
			default:
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
			}
		}

		// Token: 0x06000DED RID: 3565 RVA: 0x0002AB00 File Offset: 0x00028D00
		private static string InternalFormat(RuntimeType eT, object value)
		{
			if (eT.IsDefined(typeof(FlagsAttribute), false))
			{
				return Enum.InternalFlagsFormat(eT, value);
			}
			string name = Enum.GetName(eT, value);
			if (name == null)
			{
				return value.ToString();
			}
			return name;
		}

		// Token: 0x06000DEE RID: 3566 RVA: 0x0002AB3C File Offset: 0x00028D3C
		private static string InternalFlagsFormat(RuntimeType eT, object value)
		{
			ulong num = Enum.ToUInt64(value);
			Enum.ValuesAndNames cachedValuesAndNames = Enum.GetCachedValuesAndNames(eT, true);
			string[] names = cachedValuesAndNames.Names;
			ulong[] values = cachedValuesAndNames.Values;
			int num2 = values.Length - 1;
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = true;
			ulong num3 = num;
			while (num2 >= 0 && (num2 != 0 || values[num2] != 0UL))
			{
				if ((num & values[num2]) == values[num2])
				{
					num -= values[num2];
					if (!flag)
					{
						stringBuilder.Insert(0, ", ");
					}
					stringBuilder.Insert(0, names[num2]);
					flag = false;
				}
				num2--;
			}
			if (num != 0UL)
			{
				return value.ToString();
			}
			if (num3 != 0UL)
			{
				return stringBuilder.ToString();
			}
			if (values.Length != 0 && values[0] == 0UL)
			{
				return names[0];
			}
			return "0";
		}

		// Token: 0x06000DEF RID: 3567 RVA: 0x0002ABF0 File Offset: 0x00028DF0
		internal static ulong ToUInt64(object value)
		{
			ulong result;
			switch (Convert.GetTypeCode(value))
			{
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.Byte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
				result = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
				break;
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
				result = (ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture);
				break;
			default:
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
			}
			return result;
		}

		// Token: 0x06000DF0 RID: 3568
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int InternalCompareTo(object o1, object o2);

		// Token: 0x06000DF1 RID: 3569
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern RuntimeType InternalGetUnderlyingType(RuntimeType enumType);

		// Token: 0x06000DF2 RID: 3570
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetEnumValuesAndNames(RuntimeTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, bool getNames);

		// Token: 0x06000DF3 RID: 3571
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern object InternalBoxEnum(RuntimeType enumType, long value);

		// Token: 0x06000DF4 RID: 3572 RVA: 0x0002AC63 File Offset: 0x00028E63
		[__DynamicallyInvokable]
		public static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct
		{
			return Enum.TryParse<TEnum>(value, false, out result);
		}

		// Token: 0x06000DF5 RID: 3573 RVA: 0x0002AC70 File Offset: 0x00028E70
		[__DynamicallyInvokable]
		public static bool TryParse<TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct
		{
			result = default(TEnum);
			Enum.EnumResult enumResult = default(Enum.EnumResult);
			enumResult.Init(false);
			bool result2;
			if (result2 = Enum.TryParseEnum(typeof(TEnum), value, ignoreCase, ref enumResult))
			{
				result = (TEnum)((object)enumResult.parsedEnum);
			}
			return result2;
		}

		// Token: 0x06000DF6 RID: 3574 RVA: 0x0002ACBD File Offset: 0x00028EBD
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static object Parse(Type enumType, string value)
		{
			return Enum.Parse(enumType, value, false);
		}

		// Token: 0x06000DF7 RID: 3575 RVA: 0x0002ACC8 File Offset: 0x00028EC8
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static object Parse(Type enumType, string value, bool ignoreCase)
		{
			Enum.EnumResult enumResult = default(Enum.EnumResult);
			enumResult.Init(true);
			if (Enum.TryParseEnum(enumType, value, ignoreCase, ref enumResult))
			{
				return enumResult.parsedEnum;
			}
			throw enumResult.GetEnumParseException();
		}

		// Token: 0x06000DF8 RID: 3576 RVA: 0x0002AD00 File Offset: 0x00028F00
		private static bool TryParseEnum(Type enumType, string value, bool ignoreCase, ref Enum.EnumResult parseResult)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			if (value == null)
			{
				parseResult.SetFailure(Enum.ParseFailureKind.ArgumentNull, "value");
				return false;
			}
			value = value.Trim();
			if (value.Length == 0)
			{
				parseResult.SetFailure(Enum.ParseFailureKind.Argument, "Arg_MustContainEnumInfo", null);
				return false;
			}
			ulong num = 0UL;
			if (char.IsDigit(value[0]) || value[0] == '-' || value[0] == '+')
			{
				Type underlyingType = Enum.GetUnderlyingType(enumType);
				try
				{
					object value2 = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
					parseResult.parsedEnum = Enum.ToObject(enumType, value2);
					return true;
				}
				catch (FormatException)
				{
				}
				catch (Exception failure)
				{
					if (parseResult.canThrow)
					{
						throw;
					}
					parseResult.SetFailure(failure);
					return false;
				}
			}
			string[] array = value.Split(Enum.enumSeperatorCharArray);
			Enum.ValuesAndNames cachedValuesAndNames = Enum.GetCachedValuesAndNames(runtimeType, true);
			string[] names = cachedValuesAndNames.Names;
			ulong[] values = cachedValuesAndNames.Values;
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = array[i].Trim();
				bool flag = false;
				int j = 0;
				while (j < names.Length)
				{
					if (ignoreCase)
					{
						if (string.Compare(names[j], array[i], StringComparison.OrdinalIgnoreCase) == 0)
						{
							goto IL_15D;
						}
					}
					else if (names[j].Equals(array[i]))
					{
						goto IL_15D;
					}
					j++;
					continue;
					IL_15D:
					ulong num2 = values[j];
					num |= num2;
					flag = true;
					break;
				}
				if (!flag)
				{
					parseResult.SetFailure(Enum.ParseFailureKind.ArgumentWithParameter, "Arg_EnumValueNotFound", value);
					return false;
				}
			}
			bool result;
			try
			{
				parseResult.parsedEnum = Enum.ToObject(enumType, num);
				result = true;
			}
			catch (Exception failure2)
			{
				if (parseResult.canThrow)
				{
					throw;
				}
				parseResult.SetFailure(failure2);
				result = false;
			}
			return result;
		}

		// Token: 0x06000DF9 RID: 3577 RVA: 0x0002AF00 File Offset: 0x00029100
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static Type GetUnderlyingType(Type enumType)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			return enumType.GetEnumUnderlyingType();
		}

		// Token: 0x06000DFA RID: 3578 RVA: 0x0002AF1C File Offset: 0x0002911C
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static Array GetValues(Type enumType)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			return enumType.GetEnumValues();
		}

		// Token: 0x06000DFB RID: 3579 RVA: 0x0002AF38 File Offset: 0x00029138
		internal static ulong[] InternalGetValues(RuntimeType enumType)
		{
			return Enum.GetCachedValuesAndNames(enumType, false).Values;
		}

		// Token: 0x06000DFC RID: 3580 RVA: 0x0002AF46 File Offset: 0x00029146
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static string GetName(Type enumType, object value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			return enumType.GetEnumName(value);
		}

		// Token: 0x06000DFD RID: 3581 RVA: 0x0002AF63 File Offset: 0x00029163
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static string[] GetNames(Type enumType)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			return enumType.GetEnumNames();
		}

		// Token: 0x06000DFE RID: 3582 RVA: 0x0002AF7F File Offset: 0x0002917F
		internal static string[] InternalGetNames(RuntimeType enumType)
		{
			return Enum.GetCachedValuesAndNames(enumType, true).Names;
		}

		// Token: 0x06000DFF RID: 3583 RVA: 0x0002AF90 File Offset: 0x00029190
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static object ToObject(Type enumType, object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			TypeCode typeCode = Convert.GetTypeCode(value);
			if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && (typeCode == TypeCode.Boolean || typeCode == TypeCode.Char))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnumBaseTypeOrEnum"), "value");
			}
			switch (typeCode)
			{
			case TypeCode.Boolean:
				return Enum.ToObject(enumType, (bool)value);
			case TypeCode.Char:
				return Enum.ToObject(enumType, (char)value);
			case TypeCode.SByte:
				return Enum.ToObject(enumType, (sbyte)value);
			case TypeCode.Byte:
				return Enum.ToObject(enumType, (byte)value);
			case TypeCode.Int16:
				return Enum.ToObject(enumType, (short)value);
			case TypeCode.UInt16:
				return Enum.ToObject(enumType, (ushort)value);
			case TypeCode.Int32:
				return Enum.ToObject(enumType, (int)value);
			case TypeCode.UInt32:
				return Enum.ToObject(enumType, (uint)value);
			case TypeCode.Int64:
				return Enum.ToObject(enumType, (long)value);
			case TypeCode.UInt64:
				return Enum.ToObject(enumType, (ulong)value);
			default:
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnumBaseTypeOrEnum"), "value");
			}
		}

		// Token: 0x06000E00 RID: 3584 RVA: 0x0002B0A1 File Offset: 0x000292A1
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static bool IsDefined(Type enumType, object value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			return enumType.IsEnumDefined(value);
		}

		// Token: 0x06000E01 RID: 3585 RVA: 0x0002B0C0 File Offset: 0x000292C0
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static string Format(Type enumType, object value, string format)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (format == null)
			{
				throw new ArgumentNullException("format");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			Type type = value.GetType();
			Type underlyingType = Enum.GetUnderlyingType(enumType);
			if (type.IsEnum)
			{
				Type underlyingType2 = Enum.GetUnderlyingType(type);
				if (!type.IsEquivalentTo(enumType))
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_EnumAndObjectMustBeSameType", new object[]
					{
						type.ToString(),
						enumType.ToString()
					}));
				}
				value = ((Enum)value).GetValue();
			}
			else if (type != underlyingType)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumFormatUnderlyingTypeAndObjectMustBeSameType", new object[]
				{
					type.ToString(),
					underlyingType.ToString()
				}));
			}
			if (format.Length != 1)
			{
				throw new FormatException(Environment.GetResourceString("Format_InvalidEnumFormatSpecification"));
			}
			char c = format[0];
			if (c == 'D' || c == 'd')
			{
				return value.ToString();
			}
			if (c == 'X' || c == 'x')
			{
				return Enum.InternalFormattedHexString(value);
			}
			if (c == 'G' || c == 'g')
			{
				return Enum.InternalFormat(runtimeType, value);
			}
			if (c == 'F' || c == 'f')
			{
				return Enum.InternalFlagsFormat(runtimeType, value);
			}
			throw new FormatException(Environment.GetResourceString("Format_InvalidEnumFormatSpecification"));
		}

		// Token: 0x06000E02 RID: 3586 RVA: 0x0002B248 File Offset: 0x00029448
		[SecuritySafeCritical]
		internal unsafe object GetValue()
		{
			fixed (byte* ptr = &JitHelpers.GetPinningHelper(this).m_data)
			{
				void* ptr2 = (void*)ptr;
				switch (this.InternalGetCorElementType())
				{
				case CorElementType.Boolean:
					return *(byte*)ptr2 != 0;
				case CorElementType.Char:
					return (char)(*(ushort*)ptr2);
				case CorElementType.I1:
					return *(sbyte*)ptr2;
				case CorElementType.U1:
					return *(byte*)ptr2;
				case CorElementType.I2:
					return *(short*)ptr2;
				case CorElementType.U2:
					return *(ushort*)ptr2;
				case CorElementType.I4:
					return *(int*)ptr2;
				case CorElementType.U4:
					return *(uint*)ptr2;
				case CorElementType.I8:
					return *(long*)ptr2;
				case CorElementType.U8:
					return (ulong)(*(long*)ptr2);
				case CorElementType.R4:
					return *(float*)ptr2;
				case CorElementType.R8:
					return *(double*)ptr2;
				case CorElementType.I:
					return *(IntPtr*)ptr2;
				case CorElementType.U:
					return (UIntPtr)(*(IntPtr*)ptr2);
				}
				return null;
			}
		}

		// Token: 0x06000E03 RID: 3587
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool InternalHasFlag(Enum flags);

		// Token: 0x06000E04 RID: 3588
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern CorElementType InternalGetCorElementType();

		// Token: 0x06000E05 RID: 3589
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public override extern bool Equals(object obj);

		// Token: 0x06000E06 RID: 3590 RVA: 0x0002B348 File Offset: 0x00029548
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe override int GetHashCode()
		{
			fixed (byte* ptr = &JitHelpers.GetPinningHelper(this).m_data)
			{
				void* ptr2 = (void*)ptr;
				switch (this.InternalGetCorElementType())
				{
				case CorElementType.Boolean:
					return ((bool*)ptr2)->GetHashCode();
				case CorElementType.Char:
					return ((char*)ptr2)->GetHashCode();
				case CorElementType.I1:
					return ((sbyte*)ptr2)->GetHashCode();
				case CorElementType.U1:
					return ((byte*)ptr2)->GetHashCode();
				case CorElementType.I2:
					return ((short*)ptr2)->GetHashCode();
				case CorElementType.U2:
					return ((ushort*)ptr2)->GetHashCode();
				case CorElementType.I4:
					return ((int*)ptr2)->GetHashCode();
				case CorElementType.U4:
					return ((uint*)ptr2)->GetHashCode();
				case CorElementType.I8:
					return ((long*)ptr2)->GetHashCode();
				case CorElementType.U8:
					return ((ulong*)ptr2)->GetHashCode();
				case CorElementType.R4:
					return ((float*)ptr2)->GetHashCode();
				case CorElementType.R8:
					return ((double*)ptr2)->GetHashCode();
				case CorElementType.I:
					return ((IntPtr*)ptr2)->GetHashCode();
				case CorElementType.U:
					return ((UIntPtr*)ptr2)->GetHashCode();
				}
				return 0;
			}
		}

		// Token: 0x06000E07 RID: 3591 RVA: 0x0002B438 File Offset: 0x00029638
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return Enum.InternalFormat((RuntimeType)base.GetType(), this.GetValue());
		}

		// Token: 0x06000E08 RID: 3592 RVA: 0x0002B450 File Offset: 0x00029650
		[Obsolete("The provider argument is not used. Please use ToString(String).")]
		public string ToString(string format, IFormatProvider provider)
		{
			return this.ToString(format);
		}

		// Token: 0x06000E09 RID: 3593 RVA: 0x0002B45C File Offset: 0x0002965C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public int CompareTo(object target)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			int num = Enum.InternalCompareTo(this, target);
			if (num < 2)
			{
				return num;
			}
			if (num == 2)
			{
				Type type = base.GetType();
				Type type2 = target.GetType();
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumAndObjectMustBeSameType", new object[]
				{
					type2.ToString(),
					type.ToString()
				}));
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
		}

		// Token: 0x06000E0A RID: 3594 RVA: 0x0002B4CC File Offset: 0x000296CC
		[__DynamicallyInvokable]
		public string ToString(string format)
		{
			if (format == null || format.Length == 0)
			{
				format = "G";
			}
			if (string.Compare(format, "G", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return this.ToString();
			}
			if (string.Compare(format, "D", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return this.GetValue().ToString();
			}
			if (string.Compare(format, "X", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return Enum.InternalFormattedHexString(this.GetValue());
			}
			if (string.Compare(format, "F", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return Enum.InternalFlagsFormat((RuntimeType)base.GetType(), this.GetValue());
			}
			throw new FormatException(Environment.GetResourceString("Format_InvalidEnumFormatSpecification"));
		}

		// Token: 0x06000E0B RID: 3595 RVA: 0x0002B568 File Offset: 0x00029768
		[Obsolete("The provider argument is not used. Please use ToString().")]
		public string ToString(IFormatProvider provider)
		{
			return this.ToString();
		}

		// Token: 0x06000E0C RID: 3596 RVA: 0x0002B570 File Offset: 0x00029770
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public bool HasFlag(Enum flag)
		{
			if (flag == null)
			{
				throw new ArgumentNullException("flag");
			}
			if (!base.GetType().IsEquivalentTo(flag.GetType()))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EnumTypeDoesNotMatch", new object[]
				{
					flag.GetType(),
					base.GetType()
				}));
			}
			return this.InternalHasFlag(flag);
		}

		// Token: 0x06000E0D RID: 3597 RVA: 0x0002B5D0 File Offset: 0x000297D0
		public TypeCode GetTypeCode()
		{
			Type type = base.GetType();
			Type underlyingType = Enum.GetUnderlyingType(type);
			if (underlyingType == typeof(int))
			{
				return TypeCode.Int32;
			}
			if (underlyingType == typeof(sbyte))
			{
				return TypeCode.SByte;
			}
			if (underlyingType == typeof(short))
			{
				return TypeCode.Int16;
			}
			if (underlyingType == typeof(long))
			{
				return TypeCode.Int64;
			}
			if (underlyingType == typeof(uint))
			{
				return TypeCode.UInt32;
			}
			if (underlyingType == typeof(byte))
			{
				return TypeCode.Byte;
			}
			if (underlyingType == typeof(ushort))
			{
				return TypeCode.UInt16;
			}
			if (underlyingType == typeof(ulong))
			{
				return TypeCode.UInt64;
			}
			if (underlyingType == typeof(bool))
			{
				return TypeCode.Boolean;
			}
			if (underlyingType == typeof(char))
			{
				return TypeCode.Char;
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
		}

		// Token: 0x06000E0E RID: 3598 RVA: 0x0002B6C6 File Offset: 0x000298C6
		[__DynamicallyInvokable]
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return Convert.ToBoolean(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E0F RID: 3599 RVA: 0x0002B6D8 File Offset: 0x000298D8
		[__DynamicallyInvokable]
		char IConvertible.ToChar(IFormatProvider provider)
		{
			return Convert.ToChar(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E10 RID: 3600 RVA: 0x0002B6EA File Offset: 0x000298EA
		[__DynamicallyInvokable]
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return Convert.ToSByte(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E11 RID: 3601 RVA: 0x0002B6FC File Offset: 0x000298FC
		[__DynamicallyInvokable]
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return Convert.ToByte(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E12 RID: 3602 RVA: 0x0002B70E File Offset: 0x0002990E
		[__DynamicallyInvokable]
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return Convert.ToInt16(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E13 RID: 3603 RVA: 0x0002B720 File Offset: 0x00029920
		[__DynamicallyInvokable]
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return Convert.ToUInt16(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E14 RID: 3604 RVA: 0x0002B732 File Offset: 0x00029932
		[__DynamicallyInvokable]
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return Convert.ToInt32(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E15 RID: 3605 RVA: 0x0002B744 File Offset: 0x00029944
		[__DynamicallyInvokable]
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return Convert.ToUInt32(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E16 RID: 3606 RVA: 0x0002B756 File Offset: 0x00029956
		[__DynamicallyInvokable]
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return Convert.ToInt64(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E17 RID: 3607 RVA: 0x0002B768 File Offset: 0x00029968
		[__DynamicallyInvokable]
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return Convert.ToUInt64(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E18 RID: 3608 RVA: 0x0002B77A File Offset: 0x0002997A
		[__DynamicallyInvokable]
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return Convert.ToSingle(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E19 RID: 3609 RVA: 0x0002B78C File Offset: 0x0002998C
		[__DynamicallyInvokable]
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return Convert.ToDouble(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E1A RID: 3610 RVA: 0x0002B79E File Offset: 0x0002999E
		[__DynamicallyInvokable]
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return Convert.ToDecimal(this.GetValue(), CultureInfo.CurrentCulture);
		}

		// Token: 0x06000E1B RID: 3611 RVA: 0x0002B7B0 File Offset: 0x000299B0
		[__DynamicallyInvokable]
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", new object[] { "Enum", "DateTime" }));
		}

		// Token: 0x06000E1C RID: 3612 RVA: 0x0002B7D7 File Offset: 0x000299D7
		[__DynamicallyInvokable]
		object IConvertible.ToType(Type type, IFormatProvider provider)
		{
			return Convert.DefaultToType(this, type, provider);
		}

		// Token: 0x06000E1D RID: 3613 RVA: 0x0002B7E4 File Offset: 0x000299E4
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		[ComVisible(true)]
		public static object ToObject(Type enumType, sbyte value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			return Enum.InternalBoxEnum(runtimeType, (long)value);
		}

		// Token: 0x06000E1E RID: 3614 RVA: 0x0002B850 File Offset: 0x00029A50
		[SecuritySafeCritical]
		[ComVisible(true)]
		public static object ToObject(Type enumType, short value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			return Enum.InternalBoxEnum(runtimeType, (long)value);
		}

		// Token: 0x06000E1F RID: 3615 RVA: 0x0002B8BC File Offset: 0x00029ABC
		[SecuritySafeCritical]
		[ComVisible(true)]
		public static object ToObject(Type enumType, int value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			return Enum.InternalBoxEnum(runtimeType, (long)value);
		}

		// Token: 0x06000E20 RID: 3616 RVA: 0x0002B928 File Offset: 0x00029B28
		[SecuritySafeCritical]
		[ComVisible(true)]
		public static object ToObject(Type enumType, byte value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			return Enum.InternalBoxEnum(runtimeType, (long)((ulong)value));
		}

		// Token: 0x06000E21 RID: 3617 RVA: 0x0002B994 File Offset: 0x00029B94
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		[ComVisible(true)]
		public static object ToObject(Type enumType, ushort value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			return Enum.InternalBoxEnum(runtimeType, (long)((ulong)value));
		}

		// Token: 0x06000E22 RID: 3618 RVA: 0x0002BA00 File Offset: 0x00029C00
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		[ComVisible(true)]
		public static object ToObject(Type enumType, uint value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			return Enum.InternalBoxEnum(runtimeType, (long)((ulong)value));
		}

		// Token: 0x06000E23 RID: 3619 RVA: 0x0002BA6C File Offset: 0x00029C6C
		[SecuritySafeCritical]
		[ComVisible(true)]
		public static object ToObject(Type enumType, long value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			return Enum.InternalBoxEnum(runtimeType, value);
		}

		// Token: 0x06000E24 RID: 3620 RVA: 0x0002BAD8 File Offset: 0x00029CD8
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		[ComVisible(true)]
		public static object ToObject(Type enumType, ulong value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			return Enum.InternalBoxEnum(runtimeType, (long)value);
		}

		// Token: 0x06000E25 RID: 3621 RVA: 0x0002BB44 File Offset: 0x00029D44
		[SecuritySafeCritical]
		private static object ToObject(Type enumType, char value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			return Enum.InternalBoxEnum(runtimeType, (long)((ulong)value));
		}

		// Token: 0x06000E26 RID: 3622 RVA: 0x0002BBB0 File Offset: 0x00029DB0
		[SecuritySafeCritical]
		private static object ToObject(Type enumType, bool value)
		{
			if (enumType == null)
			{
				throw new ArgumentNullException("enumType");
			}
			if (!enumType.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			RuntimeType runtimeType = enumType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
			}
			return Enum.InternalBoxEnum(runtimeType, value ? 1L : 0L);
		}

		// Token: 0x06000E27 RID: 3623 RVA: 0x0002BC21 File Offset: 0x00029E21
		[__DynamicallyInvokable]
		protected Enum()
		{
		}

		// Token: 0x04000569 RID: 1385
		private static readonly char[] enumSeperatorCharArray = new char[] { ',' };

		// Token: 0x0400056A RID: 1386
		private const string enumSeperator = ", ";

		// Token: 0x02000AE2 RID: 2786
		private enum ParseFailureKind
		{
			// Token: 0x04003136 RID: 12598
			None,
			// Token: 0x04003137 RID: 12599
			Argument,
			// Token: 0x04003138 RID: 12600
			ArgumentNull,
			// Token: 0x04003139 RID: 12601
			ArgumentWithParameter,
			// Token: 0x0400313A RID: 12602
			UnhandledException
		}

		// Token: 0x02000AE3 RID: 2787
		private struct EnumResult
		{
			// Token: 0x060069FB RID: 27131 RVA: 0x0016D003 File Offset: 0x0016B203
			internal void Init(bool canMethodThrow)
			{
				this.parsedEnum = 0;
				this.canThrow = canMethodThrow;
			}

			// Token: 0x060069FC RID: 27132 RVA: 0x0016D018 File Offset: 0x0016B218
			internal void SetFailure(Exception unhandledException)
			{
				this.m_failure = Enum.ParseFailureKind.UnhandledException;
				this.m_innerException = unhandledException;
			}

			// Token: 0x060069FD RID: 27133 RVA: 0x0016D028 File Offset: 0x0016B228
			internal void SetFailure(Enum.ParseFailureKind failure, string failureParameter)
			{
				this.m_failure = failure;
				this.m_failureParameter = failureParameter;
				if (this.canThrow)
				{
					throw this.GetEnumParseException();
				}
			}

			// Token: 0x060069FE RID: 27134 RVA: 0x0016D047 File Offset: 0x0016B247
			internal void SetFailure(Enum.ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument)
			{
				this.m_failure = failure;
				this.m_failureMessageID = failureMessageID;
				this.m_failureMessageFormatArgument = failureMessageFormatArgument;
				if (this.canThrow)
				{
					throw this.GetEnumParseException();
				}
			}

			// Token: 0x060069FF RID: 27135 RVA: 0x0016D070 File Offset: 0x0016B270
			internal Exception GetEnumParseException()
			{
				switch (this.m_failure)
				{
				case Enum.ParseFailureKind.Argument:
					return new ArgumentException(Environment.GetResourceString(this.m_failureMessageID));
				case Enum.ParseFailureKind.ArgumentNull:
					return new ArgumentNullException(this.m_failureParameter);
				case Enum.ParseFailureKind.ArgumentWithParameter:
					return new ArgumentException(Environment.GetResourceString(this.m_failureMessageID, new object[] { this.m_failureMessageFormatArgument }));
				case Enum.ParseFailureKind.UnhandledException:
					return this.m_innerException;
				default:
					return new ArgumentException(Environment.GetResourceString("Arg_EnumValueNotFound"));
				}
			}

			// Token: 0x0400313B RID: 12603
			internal object parsedEnum;

			// Token: 0x0400313C RID: 12604
			internal bool canThrow;

			// Token: 0x0400313D RID: 12605
			internal Enum.ParseFailureKind m_failure;

			// Token: 0x0400313E RID: 12606
			internal string m_failureMessageID;

			// Token: 0x0400313F RID: 12607
			internal string m_failureParameter;

			// Token: 0x04003140 RID: 12608
			internal object m_failureMessageFormatArgument;

			// Token: 0x04003141 RID: 12609
			internal Exception m_innerException;
		}

		// Token: 0x02000AE4 RID: 2788
		private class ValuesAndNames
		{
			// Token: 0x06006A00 RID: 27136 RVA: 0x0016D0F1 File Offset: 0x0016B2F1
			public ValuesAndNames(ulong[] values, string[] names)
			{
				this.Values = values;
				this.Names = names;
			}

			// Token: 0x04003142 RID: 12610
			public ulong[] Values;

			// Token: 0x04003143 RID: 12611
			public string[] Names;
		}
	}
}
