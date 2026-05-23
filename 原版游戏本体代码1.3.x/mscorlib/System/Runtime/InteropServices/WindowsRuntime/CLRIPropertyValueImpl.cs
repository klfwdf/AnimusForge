using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A00 RID: 2560
	internal class CLRIPropertyValueImpl : IPropertyValue
	{
		// Token: 0x060064FE RID: 25854 RVA: 0x00157E7D File Offset: 0x0015607D
		internal CLRIPropertyValueImpl(PropertyType type, object data)
		{
			this._type = type;
			this._data = data;
		}

		// Token: 0x1700115E RID: 4446
		// (get) Token: 0x060064FF RID: 25855 RVA: 0x00157E94 File Offset: 0x00156094
		private static Tuple<Type, PropertyType>[] NumericScalarTypes
		{
			get
			{
				if (CLRIPropertyValueImpl.s_numericScalarTypes == null)
				{
					Tuple<Type, PropertyType>[] array = new Tuple<Type, PropertyType>[]
					{
						new Tuple<Type, PropertyType>(typeof(byte), PropertyType.UInt8),
						new Tuple<Type, PropertyType>(typeof(short), PropertyType.Int16),
						new Tuple<Type, PropertyType>(typeof(ushort), PropertyType.UInt16),
						new Tuple<Type, PropertyType>(typeof(int), PropertyType.Int32),
						new Tuple<Type, PropertyType>(typeof(uint), PropertyType.UInt32),
						new Tuple<Type, PropertyType>(typeof(long), PropertyType.Int64),
						new Tuple<Type, PropertyType>(typeof(ulong), PropertyType.UInt64),
						new Tuple<Type, PropertyType>(typeof(float), PropertyType.Single),
						new Tuple<Type, PropertyType>(typeof(double), PropertyType.Double)
					};
					CLRIPropertyValueImpl.s_numericScalarTypes = array;
				}
				return CLRIPropertyValueImpl.s_numericScalarTypes;
			}
		}

		// Token: 0x1700115F RID: 4447
		// (get) Token: 0x06006500 RID: 25856 RVA: 0x00157F70 File Offset: 0x00156170
		public PropertyType Type
		{
			get
			{
				return this._type;
			}
		}

		// Token: 0x17001160 RID: 4448
		// (get) Token: 0x06006501 RID: 25857 RVA: 0x00157F78 File Offset: 0x00156178
		public bool IsNumericScalar
		{
			get
			{
				return CLRIPropertyValueImpl.IsNumericScalarImpl(this._type, this._data);
			}
		}

		// Token: 0x06006502 RID: 25858 RVA: 0x00157F8B File Offset: 0x0015618B
		public override string ToString()
		{
			if (this._data != null)
			{
				return this._data.ToString();
			}
			return base.ToString();
		}

		// Token: 0x06006503 RID: 25859 RVA: 0x00157FA7 File Offset: 0x001561A7
		public byte GetUInt8()
		{
			return this.CoerceScalarValue<byte>(PropertyType.UInt8);
		}

		// Token: 0x06006504 RID: 25860 RVA: 0x00157FB0 File Offset: 0x001561B0
		public short GetInt16()
		{
			return this.CoerceScalarValue<short>(PropertyType.Int16);
		}

		// Token: 0x06006505 RID: 25861 RVA: 0x00157FB9 File Offset: 0x001561B9
		public ushort GetUInt16()
		{
			return this.CoerceScalarValue<ushort>(PropertyType.UInt16);
		}

		// Token: 0x06006506 RID: 25862 RVA: 0x00157FC2 File Offset: 0x001561C2
		public int GetInt32()
		{
			return this.CoerceScalarValue<int>(PropertyType.Int32);
		}

		// Token: 0x06006507 RID: 25863 RVA: 0x00157FCB File Offset: 0x001561CB
		public uint GetUInt32()
		{
			return this.CoerceScalarValue<uint>(PropertyType.UInt32);
		}

		// Token: 0x06006508 RID: 25864 RVA: 0x00157FD4 File Offset: 0x001561D4
		public long GetInt64()
		{
			return this.CoerceScalarValue<long>(PropertyType.Int64);
		}

		// Token: 0x06006509 RID: 25865 RVA: 0x00157FDD File Offset: 0x001561DD
		public ulong GetUInt64()
		{
			return this.CoerceScalarValue<ulong>(PropertyType.UInt64);
		}

		// Token: 0x0600650A RID: 25866 RVA: 0x00157FE6 File Offset: 0x001561E6
		public float GetSingle()
		{
			return this.CoerceScalarValue<float>(PropertyType.Single);
		}

		// Token: 0x0600650B RID: 25867 RVA: 0x00157FEF File Offset: 0x001561EF
		public double GetDouble()
		{
			return this.CoerceScalarValue<double>(PropertyType.Double);
		}

		// Token: 0x0600650C RID: 25868 RVA: 0x00157FFC File Offset: 0x001561FC
		public char GetChar16()
		{
			if (this.Type != PropertyType.Char16)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Char16" }), -2147316576);
			}
			return (char)this._data;
		}

		// Token: 0x0600650D RID: 25869 RVA: 0x00158050 File Offset: 0x00156250
		public bool GetBoolean()
		{
			if (this.Type != PropertyType.Boolean)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Boolean" }), -2147316576);
			}
			return (bool)this._data;
		}

		// Token: 0x0600650E RID: 25870 RVA: 0x001580A3 File Offset: 0x001562A3
		public string GetString()
		{
			return this.CoerceScalarValue<string>(PropertyType.String);
		}

		// Token: 0x0600650F RID: 25871 RVA: 0x001580B0 File Offset: 0x001562B0
		public object GetInspectable()
		{
			if (this.Type != PropertyType.Inspectable)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Inspectable" }), -2147316576);
			}
			return this._data;
		}

		// Token: 0x06006510 RID: 25872 RVA: 0x001580FE File Offset: 0x001562FE
		public Guid GetGuid()
		{
			return this.CoerceScalarValue<Guid>(PropertyType.Guid);
		}

		// Token: 0x06006511 RID: 25873 RVA: 0x00158108 File Offset: 0x00156308
		public DateTimeOffset GetDateTime()
		{
			if (this.Type != PropertyType.DateTime)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "DateTime" }), -2147316576);
			}
			return (DateTimeOffset)this._data;
		}

		// Token: 0x06006512 RID: 25874 RVA: 0x0015815C File Offset: 0x0015635C
		public TimeSpan GetTimeSpan()
		{
			if (this.Type != PropertyType.TimeSpan)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "TimeSpan" }), -2147316576);
			}
			return (TimeSpan)this._data;
		}

		// Token: 0x06006513 RID: 25875 RVA: 0x001581B0 File Offset: 0x001563B0
		[SecuritySafeCritical]
		public Point GetPoint()
		{
			if (this.Type != PropertyType.Point)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Point" }), -2147316576);
			}
			return this.Unbox<Point>(IReferenceFactory.s_pointType);
		}

		// Token: 0x06006514 RID: 25876 RVA: 0x00158204 File Offset: 0x00156404
		[SecuritySafeCritical]
		public Size GetSize()
		{
			if (this.Type != PropertyType.Size)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Size" }), -2147316576);
			}
			return this.Unbox<Size>(IReferenceFactory.s_sizeType);
		}

		// Token: 0x06006515 RID: 25877 RVA: 0x00158258 File Offset: 0x00156458
		[SecuritySafeCritical]
		public Rect GetRect()
		{
			if (this.Type != PropertyType.Rect)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Rect" }), -2147316576);
			}
			return this.Unbox<Rect>(IReferenceFactory.s_rectType);
		}

		// Token: 0x06006516 RID: 25878 RVA: 0x001582AB File Offset: 0x001564AB
		public byte[] GetUInt8Array()
		{
			return this.CoerceArrayValue<byte>(PropertyType.UInt8Array);
		}

		// Token: 0x06006517 RID: 25879 RVA: 0x001582B8 File Offset: 0x001564B8
		public short[] GetInt16Array()
		{
			return this.CoerceArrayValue<short>(PropertyType.Int16Array);
		}

		// Token: 0x06006518 RID: 25880 RVA: 0x001582C5 File Offset: 0x001564C5
		public ushort[] GetUInt16Array()
		{
			return this.CoerceArrayValue<ushort>(PropertyType.UInt16Array);
		}

		// Token: 0x06006519 RID: 25881 RVA: 0x001582D2 File Offset: 0x001564D2
		public int[] GetInt32Array()
		{
			return this.CoerceArrayValue<int>(PropertyType.Int32Array);
		}

		// Token: 0x0600651A RID: 25882 RVA: 0x001582DF File Offset: 0x001564DF
		public uint[] GetUInt32Array()
		{
			return this.CoerceArrayValue<uint>(PropertyType.UInt32Array);
		}

		// Token: 0x0600651B RID: 25883 RVA: 0x001582EC File Offset: 0x001564EC
		public long[] GetInt64Array()
		{
			return this.CoerceArrayValue<long>(PropertyType.Int64Array);
		}

		// Token: 0x0600651C RID: 25884 RVA: 0x001582F9 File Offset: 0x001564F9
		public ulong[] GetUInt64Array()
		{
			return this.CoerceArrayValue<ulong>(PropertyType.UInt64Array);
		}

		// Token: 0x0600651D RID: 25885 RVA: 0x00158306 File Offset: 0x00156506
		public float[] GetSingleArray()
		{
			return this.CoerceArrayValue<float>(PropertyType.SingleArray);
		}

		// Token: 0x0600651E RID: 25886 RVA: 0x00158313 File Offset: 0x00156513
		public double[] GetDoubleArray()
		{
			return this.CoerceArrayValue<double>(PropertyType.DoubleArray);
		}

		// Token: 0x0600651F RID: 25887 RVA: 0x00158320 File Offset: 0x00156520
		public char[] GetChar16Array()
		{
			if (this.Type != PropertyType.Char16Array)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Char16[]" }), -2147316576);
			}
			return (char[])this._data;
		}

		// Token: 0x06006520 RID: 25888 RVA: 0x00158378 File Offset: 0x00156578
		public bool[] GetBooleanArray()
		{
			if (this.Type != PropertyType.BooleanArray)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Boolean[]" }), -2147316576);
			}
			return (bool[])this._data;
		}

		// Token: 0x06006521 RID: 25889 RVA: 0x001583CE File Offset: 0x001565CE
		public string[] GetStringArray()
		{
			return this.CoerceArrayValue<string>(PropertyType.StringArray);
		}

		// Token: 0x06006522 RID: 25890 RVA: 0x001583DC File Offset: 0x001565DC
		public object[] GetInspectableArray()
		{
			if (this.Type != PropertyType.InspectableArray)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Inspectable[]" }), -2147316576);
			}
			return (object[])this._data;
		}

		// Token: 0x06006523 RID: 25891 RVA: 0x00158432 File Offset: 0x00156632
		public Guid[] GetGuidArray()
		{
			return this.CoerceArrayValue<Guid>(PropertyType.GuidArray);
		}

		// Token: 0x06006524 RID: 25892 RVA: 0x00158440 File Offset: 0x00156640
		public DateTimeOffset[] GetDateTimeArray()
		{
			if (this.Type != PropertyType.DateTimeArray)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "DateTimeOffset[]" }), -2147316576);
			}
			return (DateTimeOffset[])this._data;
		}

		// Token: 0x06006525 RID: 25893 RVA: 0x00158498 File Offset: 0x00156698
		public TimeSpan[] GetTimeSpanArray()
		{
			if (this.Type != PropertyType.TimeSpanArray)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "TimeSpan[]" }), -2147316576);
			}
			return (TimeSpan[])this._data;
		}

		// Token: 0x06006526 RID: 25894 RVA: 0x001584F0 File Offset: 0x001566F0
		[SecuritySafeCritical]
		public Point[] GetPointArray()
		{
			if (this.Type != PropertyType.PointArray)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Point[]" }), -2147316576);
			}
			return this.UnboxArray<Point>(IReferenceFactory.s_pointType);
		}

		// Token: 0x06006527 RID: 25895 RVA: 0x00158548 File Offset: 0x00156748
		[SecuritySafeCritical]
		public Size[] GetSizeArray()
		{
			if (this.Type != PropertyType.SizeArray)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Size[]" }), -2147316576);
			}
			return this.UnboxArray<Size>(IReferenceFactory.s_sizeType);
		}

		// Token: 0x06006528 RID: 25896 RVA: 0x001585A0 File Offset: 0x001567A0
		[SecuritySafeCritical]
		public Rect[] GetRectArray()
		{
			if (this.Type != PropertyType.RectArray)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[] { this.Type, "Rect[]" }), -2147316576);
			}
			return this.UnboxArray<Rect>(IReferenceFactory.s_rectType);
		}

		// Token: 0x06006529 RID: 25897 RVA: 0x001585F8 File Offset: 0x001567F8
		private T[] CoerceArrayValue<T>(PropertyType unboxType)
		{
			if (this.Type == unboxType)
			{
				return (T[])this._data;
			}
			Array array = this._data as Array;
			if (array == null)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[]
				{
					this.Type,
					typeof(T).MakeArrayType().Name
				}), -2147316576);
			}
			PropertyType type = this.Type - 1024;
			T[] array2 = new T[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				try
				{
					array2[i] = CLRIPropertyValueImpl.CoerceScalarValue<T>(type, array.GetValue(i));
				}
				catch (InvalidCastException ex)
				{
					Exception ex2 = new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueArrayCoersion", new object[]
					{
						this.Type,
						typeof(T).MakeArrayType().Name,
						i,
						ex.Message
					}), ex);
					ex2.SetErrorCode(ex._HResult);
					throw ex2;
				}
			}
			return array2;
		}

		// Token: 0x0600652A RID: 25898 RVA: 0x00158724 File Offset: 0x00156924
		private T CoerceScalarValue<T>(PropertyType unboxType)
		{
			if (this.Type == unboxType)
			{
				return (T)((object)this._data);
			}
			return CLRIPropertyValueImpl.CoerceScalarValue<T>(this.Type, this._data);
		}

		// Token: 0x0600652B RID: 25899 RVA: 0x0015874C File Offset: 0x0015694C
		private static T CoerceScalarValue<T>(PropertyType type, object value)
		{
			if (!CLRIPropertyValueImpl.IsCoercable(type, value) && type != PropertyType.Inspectable)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[]
				{
					type,
					typeof(T).Name
				}), -2147316576);
			}
			try
			{
				if (type == PropertyType.String && typeof(T) == typeof(Guid))
				{
					return (T)((object)Guid.Parse((string)value));
				}
				if (type == PropertyType.Guid && typeof(T) == typeof(string))
				{
					return (T)((object)((Guid)value).ToString("D", CultureInfo.InvariantCulture));
				}
				foreach (Tuple<Type, PropertyType> tuple in CLRIPropertyValueImpl.NumericScalarTypes)
				{
					if (tuple.Item1 == typeof(T))
					{
						return (T)((object)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture));
					}
				}
			}
			catch (FormatException)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[]
				{
					type,
					typeof(T).Name
				}), -2147316576);
			}
			catch (InvalidCastException)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[]
				{
					type,
					typeof(T).Name
				}), -2147316576);
			}
			catch (OverflowException)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueCoersion", new object[]
				{
					type,
					value,
					typeof(T).Name
				}), -2147352566);
			}
			IPropertyValue propertyValue = value as IPropertyValue;
			if (type == PropertyType.Inspectable && propertyValue != null)
			{
				if (typeof(T) == typeof(byte))
				{
					return (T)((object)propertyValue.GetUInt8());
				}
				if (typeof(T) == typeof(short))
				{
					return (T)((object)propertyValue.GetInt16());
				}
				if (typeof(T) == typeof(ushort))
				{
					return (T)((object)propertyValue.GetUInt16());
				}
				if (typeof(T) == typeof(int))
				{
					return (T)((object)propertyValue.GetUInt32());
				}
				if (typeof(T) == typeof(uint))
				{
					return (T)((object)propertyValue.GetUInt32());
				}
				if (typeof(T) == typeof(long))
				{
					return (T)((object)propertyValue.GetInt64());
				}
				if (typeof(T) == typeof(ulong))
				{
					return (T)((object)propertyValue.GetUInt64());
				}
				if (typeof(T) == typeof(float))
				{
					return (T)((object)propertyValue.GetSingle());
				}
				if (typeof(T) == typeof(double))
				{
					return (T)((object)propertyValue.GetDouble());
				}
			}
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[]
			{
				type,
				typeof(T).Name
			}), -2147316576);
		}

		// Token: 0x0600652C RID: 25900 RVA: 0x00158B20 File Offset: 0x00156D20
		private static bool IsCoercable(PropertyType type, object data)
		{
			return type == PropertyType.Guid || type == PropertyType.String || CLRIPropertyValueImpl.IsNumericScalarImpl(type, data);
		}

		// Token: 0x0600652D RID: 25901 RVA: 0x00158B38 File Offset: 0x00156D38
		private static bool IsNumericScalarImpl(PropertyType type, object data)
		{
			if (data.GetType().IsEnum)
			{
				return true;
			}
			foreach (Tuple<Type, PropertyType> tuple in CLRIPropertyValueImpl.NumericScalarTypes)
			{
				if (tuple.Item2 == type)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600652E RID: 25902 RVA: 0x00158B78 File Offset: 0x00156D78
		[SecurityCritical]
		private unsafe T Unbox<T>(Type expectedBoxedType) where T : struct
		{
			if (this._data.GetType() != expectedBoxedType)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[]
				{
					this._data.GetType(),
					expectedBoxedType.Name
				}), -2147316576);
			}
			T t = Activator.CreateInstance<T>();
			fixed (byte* ptr = &JitHelpers.GetPinningHelper(this._data).m_data)
			{
				byte* src = ptr;
				byte* dest = (byte*)(void*)JitHelpers.UnsafeCastToStackPointer<T>(ref t);
				Buffer.Memcpy(dest, src, Marshal.SizeOf<T>(t));
			}
			return t;
		}

		// Token: 0x0600652F RID: 25903 RVA: 0x00158C04 File Offset: 0x00156E04
		[SecurityCritical]
		private unsafe T[] UnboxArray<T>(Type expectedArrayElementType) where T : struct
		{
			Array array = this._data as Array;
			if (array == null || this._data.GetType().GetElementType() != expectedArrayElementType)
			{
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", new object[]
				{
					this._data.GetType(),
					expectedArrayElementType.MakeArrayType().Name
				}), -2147316576);
			}
			T[] array2 = new T[array.Length];
			if (array2.Length != 0)
			{
				fixed (byte* ptr = &JitHelpers.GetPinningHelper(array).m_data)
				{
					byte* ptr2 = ptr;
					fixed (byte* ptr3 = &JitHelpers.GetPinningHelper(array2).m_data)
					{
						byte* ptr4 = ptr3;
						byte* src = (byte*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);
						byte* dest = (byte*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement<T>(array2, 0);
						Buffer.Memcpy(dest, src, checked(Marshal.SizeOf(typeof(T)) * array2.Length));
					}
				}
			}
			return array2;
		}

		// Token: 0x04002D32 RID: 11570
		private PropertyType _type;

		// Token: 0x04002D33 RID: 11571
		private object _data;

		// Token: 0x04002D34 RID: 11572
		private static volatile Tuple<Type, PropertyType>[] s_numericScalarTypes;
	}
}
