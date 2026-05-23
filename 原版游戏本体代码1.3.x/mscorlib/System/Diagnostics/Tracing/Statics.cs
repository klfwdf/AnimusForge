using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000480 RID: 1152
	internal static class Statics
	{
		// Token: 0x060036F7 RID: 14071 RVA: 0x000D3C34 File Offset: 0x000D1E34
		public static byte[] MetadataForString(string name, int prefixSize, int suffixSize, int additionalSize)
		{
			Statics.CheckName(name);
			int num = Encoding.UTF8.GetByteCount(name) + 3 + prefixSize + suffixSize;
			byte[] array = new byte[num];
			ushort num2 = checked((ushort)(num + additionalSize));
			array[0] = (byte)num2;
			array[1] = (byte)(num2 >> 8);
			Encoding.UTF8.GetBytes(name, 0, name.Length, array, 2 + prefixSize);
			return array;
		}

		// Token: 0x060036F8 RID: 14072 RVA: 0x000D3C8C File Offset: 0x000D1E8C
		public static void EncodeTags(int tags, ref int pos, byte[] metadata)
		{
			int num = tags & 268435455;
			bool flag;
			do
			{
				byte b = (byte)((num >> 21) & 127);
				flag = (num & 2097151) != 0;
				b |= (flag ? 128 : 0);
				num <<= 7;
				if (metadata != null)
				{
					metadata[pos] = b;
				}
				pos++;
			}
			while (flag);
		}

		// Token: 0x060036F9 RID: 14073 RVA: 0x000D3CDA File Offset: 0x000D1EDA
		public static byte Combine(int settingValue, byte defaultValue)
		{
			if ((int)((byte)settingValue) != settingValue)
			{
				return defaultValue;
			}
			return (byte)settingValue;
		}

		// Token: 0x060036FA RID: 14074 RVA: 0x000D3CE5 File Offset: 0x000D1EE5
		public static byte Combine(int settingValue1, int settingValue2, byte defaultValue)
		{
			if ((int)((byte)settingValue1) == settingValue1)
			{
				return (byte)settingValue1;
			}
			if ((int)((byte)settingValue2) != settingValue2)
			{
				return defaultValue;
			}
			return (byte)settingValue2;
		}

		// Token: 0x060036FB RID: 14075 RVA: 0x000D3CF8 File Offset: 0x000D1EF8
		public static int Combine(int settingValue1, int settingValue2)
		{
			if ((int)((byte)settingValue1) != settingValue1)
			{
				return settingValue2;
			}
			return settingValue1;
		}

		// Token: 0x060036FC RID: 14076 RVA: 0x000D3D02 File Offset: 0x000D1F02
		public static void CheckName(string name)
		{
			if (name != null && 0 <= name.IndexOf('\0'))
			{
				throw new ArgumentOutOfRangeException("name");
			}
		}

		// Token: 0x060036FD RID: 14077 RVA: 0x000D3D1C File Offset: 0x000D1F1C
		public static bool ShouldOverrideFieldName(string fieldName)
		{
			return fieldName.Length <= 2 && fieldName[0] == '_';
		}

		// Token: 0x060036FE RID: 14078 RVA: 0x000D3D34 File Offset: 0x000D1F34
		public static TraceLoggingDataType MakeDataType(TraceLoggingDataType baseType, EventFieldFormat format)
		{
			return (baseType & (TraceLoggingDataType)31) | (TraceLoggingDataType)(format << 8);
		}

		// Token: 0x060036FF RID: 14079 RVA: 0x000D3D3E File Offset: 0x000D1F3E
		public static TraceLoggingDataType Format8(EventFieldFormat format, TraceLoggingDataType native)
		{
			switch (format)
			{
			case EventFieldFormat.Default:
				return native;
			case EventFieldFormat.String:
				return TraceLoggingDataType.Char8;
			case EventFieldFormat.Boolean:
				return TraceLoggingDataType.Boolean8;
			case EventFieldFormat.Hexadecimal:
				return TraceLoggingDataType.HexInt8;
			}
			return Statics.MakeDataType(native, format);
		}

		// Token: 0x06003700 RID: 14080 RVA: 0x000D3D77 File Offset: 0x000D1F77
		public static TraceLoggingDataType Format16(EventFieldFormat format, TraceLoggingDataType native)
		{
			switch (format)
			{
			case EventFieldFormat.Default:
				return native;
			case EventFieldFormat.String:
				return TraceLoggingDataType.Char16;
			case EventFieldFormat.Hexadecimal:
				return TraceLoggingDataType.HexInt16;
			}
			return Statics.MakeDataType(native, format);
		}

		// Token: 0x06003701 RID: 14081 RVA: 0x000D3DAA File Offset: 0x000D1FAA
		public static TraceLoggingDataType Format32(EventFieldFormat format, TraceLoggingDataType native)
		{
			switch (format)
			{
			case EventFieldFormat.Default:
				return native;
			case (EventFieldFormat)1:
			case EventFieldFormat.String:
				break;
			case EventFieldFormat.Boolean:
				return TraceLoggingDataType.Boolean32;
			case EventFieldFormat.Hexadecimal:
				return TraceLoggingDataType.HexInt32;
			default:
				if (format == EventFieldFormat.HResult)
				{
					return TraceLoggingDataType.HResult;
				}
				break;
			}
			return Statics.MakeDataType(native, format);
		}

		// Token: 0x06003702 RID: 14082 RVA: 0x000D3DE2 File Offset: 0x000D1FE2
		public static TraceLoggingDataType Format64(EventFieldFormat format, TraceLoggingDataType native)
		{
			if (format == EventFieldFormat.Default)
			{
				return native;
			}
			if (format != EventFieldFormat.Hexadecimal)
			{
				return Statics.MakeDataType(native, format);
			}
			return TraceLoggingDataType.HexInt64;
		}

		// Token: 0x06003703 RID: 14083 RVA: 0x000D3DF9 File Offset: 0x000D1FF9
		public static TraceLoggingDataType FormatPtr(EventFieldFormat format, TraceLoggingDataType native)
		{
			if (format == EventFieldFormat.Default)
			{
				return native;
			}
			if (format != EventFieldFormat.Hexadecimal)
			{
				return Statics.MakeDataType(native, format);
			}
			return Statics.HexIntPtrType;
		}

		// Token: 0x06003704 RID: 14084 RVA: 0x000D3E13 File Offset: 0x000D2013
		public static object CreateInstance(Type type, params object[] parameters)
		{
			return Activator.CreateInstance(type, parameters);
		}

		// Token: 0x06003705 RID: 14085 RVA: 0x000D3E1C File Offset: 0x000D201C
		public static bool IsValueType(Type type)
		{
			return type.IsValueType;
		}

		// Token: 0x06003706 RID: 14086 RVA: 0x000D3E34 File Offset: 0x000D2034
		public static bool IsEnum(Type type)
		{
			return type.IsEnum;
		}

		// Token: 0x06003707 RID: 14087 RVA: 0x000D3E4C File Offset: 0x000D204C
		public static IEnumerable<PropertyInfo> GetProperties(Type type)
		{
			return type.GetProperties();
		}

		// Token: 0x06003708 RID: 14088 RVA: 0x000D3E64 File Offset: 0x000D2064
		public static MethodInfo GetGetMethod(PropertyInfo propInfo)
		{
			return propInfo.GetGetMethod();
		}

		// Token: 0x06003709 RID: 14089 RVA: 0x000D3E7C File Offset: 0x000D207C
		public static MethodInfo GetDeclaredStaticMethod(Type declaringType, string name)
		{
			return declaringType.GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
		}

		// Token: 0x0600370A RID: 14090 RVA: 0x000D3E94 File Offset: 0x000D2094
		public static bool HasCustomAttribute(PropertyInfo propInfo, Type attributeType)
		{
			object[] customAttributes = propInfo.GetCustomAttributes(attributeType, false);
			return customAttributes.Length != 0;
		}

		// Token: 0x0600370B RID: 14091 RVA: 0x000D3EB4 File Offset: 0x000D20B4
		public static AttributeType GetCustomAttribute<AttributeType>(PropertyInfo propInfo) where AttributeType : Attribute
		{
			AttributeType result = default(AttributeType);
			object[] customAttributes = propInfo.GetCustomAttributes(typeof(AttributeType), false);
			if (customAttributes.Length != 0)
			{
				result = (AttributeType)((object)customAttributes[0]);
			}
			return result;
		}

		// Token: 0x0600370C RID: 14092 RVA: 0x000D3EEC File Offset: 0x000D20EC
		public static AttributeType GetCustomAttribute<AttributeType>(Type type) where AttributeType : Attribute
		{
			AttributeType result = default(AttributeType);
			object[] customAttributes = type.GetCustomAttributes(typeof(AttributeType), false);
			if (customAttributes.Length != 0)
			{
				result = (AttributeType)((object)customAttributes[0]);
			}
			return result;
		}

		// Token: 0x0600370D RID: 14093 RVA: 0x000D3F21 File Offset: 0x000D2121
		public static Type[] GetGenericArguments(Type type)
		{
			return type.GetGenericArguments();
		}

		// Token: 0x0600370E RID: 14094 RVA: 0x000D3F2C File Offset: 0x000D212C
		public static Type FindEnumerableElementType(Type type)
		{
			Type type2 = null;
			if (Statics.IsGenericMatch(type, typeof(IEnumerable<>)))
			{
				type2 = Statics.GetGenericArguments(type)[0];
			}
			else
			{
				Type[] array = type.FindInterfaces(new TypeFilter(Statics.IsGenericMatch), typeof(IEnumerable<>));
				foreach (Type type3 in array)
				{
					if (type2 != null)
					{
						type2 = null;
						break;
					}
					type2 = Statics.GetGenericArguments(type3)[0];
				}
			}
			return type2;
		}

		// Token: 0x0600370F RID: 14095 RVA: 0x000D3FA4 File Offset: 0x000D21A4
		public static bool IsGenericMatch(Type type, object openType)
		{
			bool isGenericType = type.IsGenericType;
			return isGenericType && type.GetGenericTypeDefinition() == (Type)openType;
		}

		// Token: 0x06003710 RID: 14096 RVA: 0x000D3FD0 File Offset: 0x000D21D0
		public static Delegate CreateDelegate(Type delegateType, MethodInfo methodInfo)
		{
			return Delegate.CreateDelegate(delegateType, methodInfo);
		}

		// Token: 0x06003711 RID: 14097 RVA: 0x000D3FE8 File Offset: 0x000D21E8
		public static TraceLoggingTypeInfo GetTypeInfoInstance(Type dataType, List<Type> recursionCheck)
		{
			TraceLoggingTypeInfo result;
			if (dataType == typeof(int))
			{
				result = TraceLoggingTypeInfo<int>.Instance;
			}
			else if (dataType == typeof(long))
			{
				result = TraceLoggingTypeInfo<long>.Instance;
			}
			else if (dataType == typeof(string))
			{
				result = TraceLoggingTypeInfo<string>.Instance;
			}
			else
			{
				MethodInfo declaredStaticMethod = Statics.GetDeclaredStaticMethod(typeof(TraceLoggingTypeInfo<>).MakeGenericType(new Type[] { dataType }), "GetInstance");
				object obj = declaredStaticMethod.Invoke(null, new object[] { recursionCheck });
				result = (TraceLoggingTypeInfo)obj;
			}
			return result;
		}

		// Token: 0x06003712 RID: 14098 RVA: 0x000D4084 File Offset: 0x000D2284
		public static TraceLoggingTypeInfo<DataType> CreateDefaultTypeInfo<DataType>(List<Type> recursionCheck)
		{
			Type typeFromHandle = typeof(DataType);
			if (recursionCheck.Contains(typeFromHandle))
			{
				throw new NotSupportedException(Environment.GetResourceString("EventSource_RecursiveTypeDefinition"));
			}
			recursionCheck.Add(typeFromHandle);
			EventDataAttribute customAttribute = Statics.GetCustomAttribute<EventDataAttribute>(typeFromHandle);
			TraceLoggingTypeInfo traceLoggingTypeInfo;
			if (customAttribute != null || Statics.GetCustomAttribute<CompilerGeneratedAttribute>(typeFromHandle) != null)
			{
				TypeAnalysis typeAnalysis = new TypeAnalysis(typeFromHandle, customAttribute, recursionCheck);
				traceLoggingTypeInfo = new InvokeTypeInfo<DataType>(typeAnalysis);
			}
			else if (typeFromHandle.IsArray)
			{
				Type elementType = typeFromHandle.GetElementType();
				if (elementType == typeof(bool))
				{
					traceLoggingTypeInfo = new BooleanArrayTypeInfo();
				}
				else if (elementType == typeof(byte))
				{
					traceLoggingTypeInfo = new ByteArrayTypeInfo();
				}
				else if (elementType == typeof(sbyte))
				{
					traceLoggingTypeInfo = new SByteArrayTypeInfo();
				}
				else if (elementType == typeof(short))
				{
					traceLoggingTypeInfo = new Int16ArrayTypeInfo();
				}
				else if (elementType == typeof(ushort))
				{
					traceLoggingTypeInfo = new UInt16ArrayTypeInfo();
				}
				else if (elementType == typeof(int))
				{
					traceLoggingTypeInfo = new Int32ArrayTypeInfo();
				}
				else if (elementType == typeof(uint))
				{
					traceLoggingTypeInfo = new UInt32ArrayTypeInfo();
				}
				else if (elementType == typeof(long))
				{
					traceLoggingTypeInfo = new Int64ArrayTypeInfo();
				}
				else if (elementType == typeof(ulong))
				{
					traceLoggingTypeInfo = new UInt64ArrayTypeInfo();
				}
				else if (elementType == typeof(char))
				{
					traceLoggingTypeInfo = new CharArrayTypeInfo();
				}
				else if (elementType == typeof(double))
				{
					traceLoggingTypeInfo = new DoubleArrayTypeInfo();
				}
				else if (elementType == typeof(float))
				{
					traceLoggingTypeInfo = new SingleArrayTypeInfo();
				}
				else if (elementType == typeof(IntPtr))
				{
					traceLoggingTypeInfo = new IntPtrArrayTypeInfo();
				}
				else if (elementType == typeof(UIntPtr))
				{
					traceLoggingTypeInfo = new UIntPtrArrayTypeInfo();
				}
				else if (elementType == typeof(Guid))
				{
					traceLoggingTypeInfo = new GuidArrayTypeInfo();
				}
				else
				{
					traceLoggingTypeInfo = (TraceLoggingTypeInfo<DataType>)Statics.CreateInstance(typeof(ArrayTypeInfo<>).MakeGenericType(new Type[] { elementType }), new object[] { Statics.GetTypeInfoInstance(elementType, recursionCheck) });
				}
			}
			else if (Statics.IsEnum(typeFromHandle))
			{
				Type underlyingType = Enum.GetUnderlyingType(typeFromHandle);
				if (underlyingType == typeof(int))
				{
					traceLoggingTypeInfo = new EnumInt32TypeInfo<DataType>();
				}
				else if (underlyingType == typeof(uint))
				{
					traceLoggingTypeInfo = new EnumUInt32TypeInfo<DataType>();
				}
				else if (underlyingType == typeof(byte))
				{
					traceLoggingTypeInfo = new EnumByteTypeInfo<DataType>();
				}
				else if (underlyingType == typeof(sbyte))
				{
					traceLoggingTypeInfo = new EnumSByteTypeInfo<DataType>();
				}
				else if (underlyingType == typeof(short))
				{
					traceLoggingTypeInfo = new EnumInt16TypeInfo<DataType>();
				}
				else if (underlyingType == typeof(ushort))
				{
					traceLoggingTypeInfo = new EnumUInt16TypeInfo<DataType>();
				}
				else if (underlyingType == typeof(long))
				{
					traceLoggingTypeInfo = new EnumInt64TypeInfo<DataType>();
				}
				else
				{
					if (!(underlyingType == typeof(ulong)))
					{
						throw new NotSupportedException(Environment.GetResourceString("EventSource_NotSupportedEnumType", new object[] { typeFromHandle.Name, underlyingType.Name }));
					}
					traceLoggingTypeInfo = new EnumUInt64TypeInfo<DataType>();
				}
			}
			else if (typeFromHandle == typeof(string))
			{
				traceLoggingTypeInfo = new StringTypeInfo();
			}
			else if (typeFromHandle == typeof(bool))
			{
				traceLoggingTypeInfo = new BooleanTypeInfo();
			}
			else if (typeFromHandle == typeof(byte))
			{
				traceLoggingTypeInfo = new ByteTypeInfo();
			}
			else if (typeFromHandle == typeof(sbyte))
			{
				traceLoggingTypeInfo = new SByteTypeInfo();
			}
			else if (typeFromHandle == typeof(short))
			{
				traceLoggingTypeInfo = new Int16TypeInfo();
			}
			else if (typeFromHandle == typeof(ushort))
			{
				traceLoggingTypeInfo = new UInt16TypeInfo();
			}
			else if (typeFromHandle == typeof(int))
			{
				traceLoggingTypeInfo = new Int32TypeInfo();
			}
			else if (typeFromHandle == typeof(uint))
			{
				traceLoggingTypeInfo = new UInt32TypeInfo();
			}
			else if (typeFromHandle == typeof(long))
			{
				traceLoggingTypeInfo = new Int64TypeInfo();
			}
			else if (typeFromHandle == typeof(ulong))
			{
				traceLoggingTypeInfo = new UInt64TypeInfo();
			}
			else if (typeFromHandle == typeof(char))
			{
				traceLoggingTypeInfo = new CharTypeInfo();
			}
			else if (typeFromHandle == typeof(double))
			{
				traceLoggingTypeInfo = new DoubleTypeInfo();
			}
			else if (typeFromHandle == typeof(float))
			{
				traceLoggingTypeInfo = new SingleTypeInfo();
			}
			else if (typeFromHandle == typeof(DateTime))
			{
				traceLoggingTypeInfo = new DateTimeTypeInfo();
			}
			else if (typeFromHandle == typeof(decimal))
			{
				traceLoggingTypeInfo = new DecimalTypeInfo();
			}
			else if (typeFromHandle == typeof(IntPtr))
			{
				traceLoggingTypeInfo = new IntPtrTypeInfo();
			}
			else if (typeFromHandle == typeof(UIntPtr))
			{
				traceLoggingTypeInfo = new UIntPtrTypeInfo();
			}
			else if (typeFromHandle == typeof(Guid))
			{
				traceLoggingTypeInfo = new GuidTypeInfo();
			}
			else if (typeFromHandle == typeof(TimeSpan))
			{
				traceLoggingTypeInfo = new TimeSpanTypeInfo();
			}
			else if (typeFromHandle == typeof(DateTimeOffset))
			{
				traceLoggingTypeInfo = new DateTimeOffsetTypeInfo();
			}
			else if (typeFromHandle == typeof(EmptyStruct))
			{
				traceLoggingTypeInfo = new NullTypeInfo<EmptyStruct>();
			}
			else if (Statics.IsGenericMatch(typeFromHandle, typeof(KeyValuePair<, >)))
			{
				Type[] genericArguments = Statics.GetGenericArguments(typeFromHandle);
				traceLoggingTypeInfo = (TraceLoggingTypeInfo<DataType>)Statics.CreateInstance(typeof(KeyValuePairTypeInfo<, >).MakeGenericType(new Type[]
				{
					genericArguments[0],
					genericArguments[1]
				}), new object[] { recursionCheck });
			}
			else if (Statics.IsGenericMatch(typeFromHandle, typeof(Nullable<>)))
			{
				Type[] genericArguments2 = Statics.GetGenericArguments(typeFromHandle);
				traceLoggingTypeInfo = (TraceLoggingTypeInfo<DataType>)Statics.CreateInstance(typeof(NullableTypeInfo<>).MakeGenericType(new Type[] { genericArguments2[0] }), new object[] { recursionCheck });
			}
			else
			{
				Type type = Statics.FindEnumerableElementType(typeFromHandle);
				if (!(type != null))
				{
					throw new ArgumentException(Environment.GetResourceString("EventSource_NonCompliantTypeError", new object[] { typeFromHandle.Name }));
				}
				traceLoggingTypeInfo = (TraceLoggingTypeInfo<DataType>)Statics.CreateInstance(typeof(EnumerableTypeInfo<, >).MakeGenericType(new Type[] { typeFromHandle, type }), new object[] { Statics.GetTypeInfoInstance(type, recursionCheck) });
			}
			return (TraceLoggingTypeInfo<DataType>)traceLoggingTypeInfo;
		}

		// Token: 0x04001860 RID: 6240
		public const byte DefaultLevel = 5;

		// Token: 0x04001861 RID: 6241
		public const byte TraceLoggingChannel = 11;

		// Token: 0x04001862 RID: 6242
		public const byte InTypeMask = 31;

		// Token: 0x04001863 RID: 6243
		public const byte InTypeFixedCountFlag = 32;

		// Token: 0x04001864 RID: 6244
		public const byte InTypeVariableCountFlag = 64;

		// Token: 0x04001865 RID: 6245
		public const byte InTypeCustomCountFlag = 96;

		// Token: 0x04001866 RID: 6246
		public const byte InTypeCountMask = 96;

		// Token: 0x04001867 RID: 6247
		public const byte InTypeChainFlag = 128;

		// Token: 0x04001868 RID: 6248
		public const byte OutTypeMask = 127;

		// Token: 0x04001869 RID: 6249
		public const byte OutTypeChainFlag = 128;

		// Token: 0x0400186A RID: 6250
		public const EventTags EventTagsMask = (EventTags)268435455;

		// Token: 0x0400186B RID: 6251
		public static readonly TraceLoggingDataType IntPtrType = ((IntPtr.Size == 8) ? TraceLoggingDataType.Int64 : TraceLoggingDataType.Int32);

		// Token: 0x0400186C RID: 6252
		public static readonly TraceLoggingDataType UIntPtrType = ((IntPtr.Size == 8) ? TraceLoggingDataType.UInt64 : TraceLoggingDataType.UInt32);

		// Token: 0x0400186D RID: 6253
		public static readonly TraceLoggingDataType HexIntPtrType = ((IntPtr.Size == 8) ? TraceLoggingDataType.HexInt64 : TraceLoggingDataType.HexInt32);
	}
}
