using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Security;

namespace System
{
	// Token: 0x0200015A RID: 346
	[Serializable]
	internal struct Variant
	{
		// Token: 0x06001577 RID: 5495
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern double GetR8FromVar();

		// Token: 0x06001578 RID: 5496
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern float GetR4FromVar();

		// Token: 0x06001579 RID: 5497
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern void SetFieldsR4(float val);

		// Token: 0x0600157A RID: 5498
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern void SetFieldsR8(double val);

		// Token: 0x0600157B RID: 5499
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern void SetFieldsObject(object val);

		// Token: 0x0600157C RID: 5500 RVA: 0x0003ED8E File Offset: 0x0003CF8E
		internal long GetI8FromVar()
		{
			return ((long)this.m_data2 << 32) | ((long)this.m_data1 & (long)((ulong)(-1)));
		}

		// Token: 0x0600157D RID: 5501 RVA: 0x0003EDA5 File Offset: 0x0003CFA5
		internal Variant(int flags, object or, int data1, int data2)
		{
			this.m_flags = flags;
			this.m_objref = or;
			this.m_data1 = data1;
			this.m_data2 = data2;
		}

		// Token: 0x0600157E RID: 5502 RVA: 0x0003EDC4 File Offset: 0x0003CFC4
		public Variant(bool val)
		{
			this.m_objref = null;
			this.m_flags = 2;
			this.m_data1 = (val ? 1 : 0);
			this.m_data2 = 0;
		}

		// Token: 0x0600157F RID: 5503 RVA: 0x0003EDE8 File Offset: 0x0003CFE8
		public Variant(sbyte val)
		{
			this.m_objref = null;
			this.m_flags = 4;
			this.m_data1 = (int)val;
			this.m_data2 = (int)((long)val >> 32);
		}

		// Token: 0x06001580 RID: 5504 RVA: 0x0003EE0B File Offset: 0x0003D00B
		public Variant(byte val)
		{
			this.m_objref = null;
			this.m_flags = 5;
			this.m_data1 = (int)val;
			this.m_data2 = 0;
		}

		// Token: 0x06001581 RID: 5505 RVA: 0x0003EE29 File Offset: 0x0003D029
		public Variant(short val)
		{
			this.m_objref = null;
			this.m_flags = 6;
			this.m_data1 = (int)val;
			this.m_data2 = (int)((long)val >> 32);
		}

		// Token: 0x06001582 RID: 5506 RVA: 0x0003EE4C File Offset: 0x0003D04C
		public Variant(ushort val)
		{
			this.m_objref = null;
			this.m_flags = 7;
			this.m_data1 = (int)val;
			this.m_data2 = 0;
		}

		// Token: 0x06001583 RID: 5507 RVA: 0x0003EE6A File Offset: 0x0003D06A
		public Variant(char val)
		{
			this.m_objref = null;
			this.m_flags = 3;
			this.m_data1 = (int)val;
			this.m_data2 = 0;
		}

		// Token: 0x06001584 RID: 5508 RVA: 0x0003EE88 File Offset: 0x0003D088
		public Variant(int val)
		{
			this.m_objref = null;
			this.m_flags = 8;
			this.m_data1 = val;
			this.m_data2 = val >> 31;
		}

		// Token: 0x06001585 RID: 5509 RVA: 0x0003EEA9 File Offset: 0x0003D0A9
		public Variant(uint val)
		{
			this.m_objref = null;
			this.m_flags = 9;
			this.m_data1 = (int)val;
			this.m_data2 = 0;
		}

		// Token: 0x06001586 RID: 5510 RVA: 0x0003EEC8 File Offset: 0x0003D0C8
		public Variant(long val)
		{
			this.m_objref = null;
			this.m_flags = 10;
			this.m_data1 = (int)val;
			this.m_data2 = (int)(val >> 32);
		}

		// Token: 0x06001587 RID: 5511 RVA: 0x0003EEEC File Offset: 0x0003D0EC
		public Variant(ulong val)
		{
			this.m_objref = null;
			this.m_flags = 11;
			this.m_data1 = (int)val;
			this.m_data2 = (int)(val >> 32);
		}

		// Token: 0x06001588 RID: 5512 RVA: 0x0003EF10 File Offset: 0x0003D110
		[SecuritySafeCritical]
		public Variant(float val)
		{
			this.m_objref = null;
			this.m_flags = 12;
			this.m_data1 = 0;
			this.m_data2 = 0;
			this.SetFieldsR4(val);
		}

		// Token: 0x06001589 RID: 5513 RVA: 0x0003EF36 File Offset: 0x0003D136
		[SecurityCritical]
		public Variant(double val)
		{
			this.m_objref = null;
			this.m_flags = 13;
			this.m_data1 = 0;
			this.m_data2 = 0;
			this.SetFieldsR8(val);
		}

		// Token: 0x0600158A RID: 5514 RVA: 0x0003EF5C File Offset: 0x0003D15C
		public Variant(DateTime val)
		{
			this.m_objref = null;
			this.m_flags = 16;
			ulong ticks = (ulong)val.Ticks;
			this.m_data1 = (int)ticks;
			this.m_data2 = (int)(ticks >> 32);
		}

		// Token: 0x0600158B RID: 5515 RVA: 0x0003EF93 File Offset: 0x0003D193
		public Variant(decimal val)
		{
			this.m_objref = val;
			this.m_flags = 19;
			this.m_data1 = 0;
			this.m_data2 = 0;
		}

		// Token: 0x0600158C RID: 5516 RVA: 0x0003EFB8 File Offset: 0x0003D1B8
		[SecuritySafeCritical]
		public Variant(object obj)
		{
			this.m_data1 = 0;
			this.m_data2 = 0;
			VarEnum varEnum = VarEnum.VT_EMPTY;
			if (obj is DateTime)
			{
				this.m_objref = null;
				this.m_flags = 16;
				ulong ticks = (ulong)((DateTime)obj).Ticks;
				this.m_data1 = (int)ticks;
				this.m_data2 = (int)(ticks >> 32);
				return;
			}
			if (obj is string)
			{
				this.m_flags = 14;
				this.m_objref = obj;
				return;
			}
			if (obj == null)
			{
				this = System.Variant.Empty;
				return;
			}
			if (obj == System.DBNull.Value)
			{
				this = System.Variant.DBNull;
				return;
			}
			if (obj == Type.Missing)
			{
				this = System.Variant.Missing;
				return;
			}
			if (obj is Array)
			{
				this.m_flags = 65554;
				this.m_objref = obj;
				return;
			}
			this.m_flags = 0;
			this.m_objref = null;
			if (obj is UnknownWrapper)
			{
				varEnum = VarEnum.VT_UNKNOWN;
				obj = ((UnknownWrapper)obj).WrappedObject;
			}
			else if (obj is DispatchWrapper)
			{
				varEnum = VarEnum.VT_DISPATCH;
				obj = ((DispatchWrapper)obj).WrappedObject;
			}
			else if (obj is ErrorWrapper)
			{
				varEnum = VarEnum.VT_ERROR;
				obj = ((ErrorWrapper)obj).ErrorCode;
			}
			else if (obj is CurrencyWrapper)
			{
				varEnum = VarEnum.VT_CY;
				obj = ((CurrencyWrapper)obj).WrappedObject;
			}
			else if (obj is BStrWrapper)
			{
				varEnum = VarEnum.VT_BSTR;
				obj = ((BStrWrapper)obj).WrappedObject;
			}
			if (obj != null)
			{
				this.SetFieldsObject(obj);
			}
			if (varEnum != VarEnum.VT_EMPTY)
			{
				this.m_flags |= (int)((int)varEnum << 24);
			}
		}

		// Token: 0x0600158D RID: 5517 RVA: 0x0003F130 File Offset: 0x0003D330
		[SecurityCritical]
		public unsafe Variant(void* voidPointer, Type pointerType)
		{
			if (pointerType == null)
			{
				throw new ArgumentNullException("pointerType");
			}
			if (!pointerType.IsPointer)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"), "pointerType");
			}
			this.m_objref = pointerType;
			this.m_flags = 15;
			this.m_data1 = voidPointer;
			this.m_data2 = 0;
		}

		// Token: 0x17000263 RID: 611
		// (get) Token: 0x0600158E RID: 5518 RVA: 0x0003F18C File Offset: 0x0003D38C
		internal int CVType
		{
			get
			{
				return this.m_flags & 65535;
			}
		}

		// Token: 0x0600158F RID: 5519 RVA: 0x0003F19C File Offset: 0x0003D39C
		[SecuritySafeCritical]
		public object ToObject()
		{
			switch (this.CVType)
			{
			case 0:
				return null;
			case 2:
				return this.m_data1 != 0;
			case 3:
				return (char)this.m_data1;
			case 4:
				return (sbyte)this.m_data1;
			case 5:
				return (byte)this.m_data1;
			case 6:
				return (short)this.m_data1;
			case 7:
				return (ushort)this.m_data1;
			case 8:
				return this.m_data1;
			case 9:
				return (uint)this.m_data1;
			case 10:
				return this.GetI8FromVar();
			case 11:
				return (ulong)this.GetI8FromVar();
			case 12:
				return this.GetR4FromVar();
			case 13:
				return this.GetR8FromVar();
			case 16:
				return new DateTime(this.GetI8FromVar());
			case 17:
				return new TimeSpan(this.GetI8FromVar());
			case 21:
				return this.BoxEnum();
			case 22:
				return Type.Missing;
			case 23:
				return System.DBNull.Value;
			}
			return this.m_objref;
		}

		// Token: 0x06001590 RID: 5520
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern object BoxEnum();

		// Token: 0x06001591 RID: 5521 RVA: 0x0003F2F0 File Offset: 0x0003D4F0
		[SecuritySafeCritical]
		internal static void MarshalHelperConvertObjectToVariant(object o, ref System.Variant v)
		{
			IConvertible convertible = (RemotingServices.IsTransparentProxy(o) ? null : (o as IConvertible));
			if (o == null)
			{
				v = System.Variant.Empty;
				return;
			}
			if (convertible == null)
			{
				v = new System.Variant(o);
				return;
			}
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			switch (convertible.GetTypeCode())
			{
			case TypeCode.Empty:
				v = System.Variant.Empty;
				return;
			case TypeCode.Object:
				v = new System.Variant(o);
				return;
			case TypeCode.DBNull:
				v = System.Variant.DBNull;
				return;
			case TypeCode.Boolean:
				v = new System.Variant(convertible.ToBoolean(invariantCulture));
				return;
			case TypeCode.Char:
				v = new System.Variant(convertible.ToChar(invariantCulture));
				return;
			case TypeCode.SByte:
				v = new System.Variant(convertible.ToSByte(invariantCulture));
				return;
			case TypeCode.Byte:
				v = new System.Variant(convertible.ToByte(invariantCulture));
				return;
			case TypeCode.Int16:
				v = new System.Variant(convertible.ToInt16(invariantCulture));
				return;
			case TypeCode.UInt16:
				v = new System.Variant(convertible.ToUInt16(invariantCulture));
				return;
			case TypeCode.Int32:
				v = new System.Variant(convertible.ToInt32(invariantCulture));
				return;
			case TypeCode.UInt32:
				v = new System.Variant(convertible.ToUInt32(invariantCulture));
				return;
			case TypeCode.Int64:
				v = new System.Variant(convertible.ToInt64(invariantCulture));
				return;
			case TypeCode.UInt64:
				v = new System.Variant(convertible.ToUInt64(invariantCulture));
				return;
			case TypeCode.Single:
				v = new System.Variant(convertible.ToSingle(invariantCulture));
				return;
			case TypeCode.Double:
				v = new System.Variant(convertible.ToDouble(invariantCulture));
				return;
			case TypeCode.Decimal:
				v = new System.Variant(convertible.ToDecimal(invariantCulture));
				return;
			case TypeCode.DateTime:
				v = new System.Variant(convertible.ToDateTime(invariantCulture));
				return;
			case TypeCode.String:
				v = new System.Variant(convertible.ToString(invariantCulture));
				return;
			}
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnknownTypeCode", new object[] { convertible.GetTypeCode() }));
		}

		// Token: 0x06001592 RID: 5522 RVA: 0x0003F4F7 File Offset: 0x0003D6F7
		internal static object MarshalHelperConvertVariantToObject(ref System.Variant v)
		{
			return v.ToObject();
		}

		// Token: 0x06001593 RID: 5523 RVA: 0x0003F500 File Offset: 0x0003D700
		[SecurityCritical]
		internal static void MarshalHelperCastVariant(object pValue, int vt, ref System.Variant v)
		{
			IConvertible convertible = pValue as IConvertible;
			if (convertible == null)
			{
				switch (vt)
				{
				case 8:
					if (pValue == null)
					{
						v = new System.Variant(null);
						v.m_flags = 14;
						return;
					}
					throw new InvalidCastException(Environment.GetResourceString("InvalidCast_CannotCoerceByRefVariant"));
				case 9:
					v = new System.Variant(new DispatchWrapper(pValue));
					return;
				case 10:
				case 11:
					break;
				case 12:
					v = new System.Variant(pValue);
					return;
				case 13:
					v = new System.Variant(new UnknownWrapper(pValue));
					return;
				default:
					if (vt == 36)
					{
						v = new System.Variant(pValue);
						return;
					}
					break;
				}
				throw new InvalidCastException(Environment.GetResourceString("InvalidCast_CannotCoerceByRefVariant"));
			}
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			switch (vt)
			{
			case 0:
				v = System.Variant.Empty;
				return;
			case 1:
				v = System.Variant.DBNull;
				return;
			case 2:
				v = new System.Variant(convertible.ToInt16(invariantCulture));
				return;
			case 3:
				v = new System.Variant(convertible.ToInt32(invariantCulture));
				return;
			case 4:
				v = new System.Variant(convertible.ToSingle(invariantCulture));
				return;
			case 5:
				v = new System.Variant(convertible.ToDouble(invariantCulture));
				return;
			case 6:
				v = new System.Variant(new CurrencyWrapper(convertible.ToDecimal(invariantCulture)));
				return;
			case 7:
				v = new System.Variant(convertible.ToDateTime(invariantCulture));
				return;
			case 8:
				v = new System.Variant(convertible.ToString(invariantCulture));
				return;
			case 9:
				v = new System.Variant(new DispatchWrapper(convertible));
				return;
			case 10:
				v = new System.Variant(new ErrorWrapper(convertible.ToInt32(invariantCulture)));
				return;
			case 11:
				v = new System.Variant(convertible.ToBoolean(invariantCulture));
				return;
			case 12:
				v = new System.Variant(convertible);
				return;
			case 13:
				v = new System.Variant(new UnknownWrapper(convertible));
				return;
			case 14:
				v = new System.Variant(convertible.ToDecimal(invariantCulture));
				return;
			case 16:
				v = new System.Variant(convertible.ToSByte(invariantCulture));
				return;
			case 17:
				v = new System.Variant(convertible.ToByte(invariantCulture));
				return;
			case 18:
				v = new System.Variant(convertible.ToUInt16(invariantCulture));
				return;
			case 19:
				v = new System.Variant(convertible.ToUInt32(invariantCulture));
				return;
			case 20:
				v = new System.Variant(convertible.ToInt64(invariantCulture));
				return;
			case 21:
				v = new System.Variant(convertible.ToUInt64(invariantCulture));
				return;
			case 22:
				v = new System.Variant(convertible.ToInt32(invariantCulture));
				return;
			case 23:
				v = new System.Variant(convertible.ToUInt32(invariantCulture));
				return;
			}
			throw new InvalidCastException(Environment.GetResourceString("InvalidCast_CannotCoerceByRefVariant"));
		}

		// Token: 0x04000715 RID: 1813
		private object m_objref;

		// Token: 0x04000716 RID: 1814
		private int m_data1;

		// Token: 0x04000717 RID: 1815
		private int m_data2;

		// Token: 0x04000718 RID: 1816
		private int m_flags;

		// Token: 0x04000719 RID: 1817
		internal const int CV_EMPTY = 0;

		// Token: 0x0400071A RID: 1818
		internal const int CV_VOID = 1;

		// Token: 0x0400071B RID: 1819
		internal const int CV_BOOLEAN = 2;

		// Token: 0x0400071C RID: 1820
		internal const int CV_CHAR = 3;

		// Token: 0x0400071D RID: 1821
		internal const int CV_I1 = 4;

		// Token: 0x0400071E RID: 1822
		internal const int CV_U1 = 5;

		// Token: 0x0400071F RID: 1823
		internal const int CV_I2 = 6;

		// Token: 0x04000720 RID: 1824
		internal const int CV_U2 = 7;

		// Token: 0x04000721 RID: 1825
		internal const int CV_I4 = 8;

		// Token: 0x04000722 RID: 1826
		internal const int CV_U4 = 9;

		// Token: 0x04000723 RID: 1827
		internal const int CV_I8 = 10;

		// Token: 0x04000724 RID: 1828
		internal const int CV_U8 = 11;

		// Token: 0x04000725 RID: 1829
		internal const int CV_R4 = 12;

		// Token: 0x04000726 RID: 1830
		internal const int CV_R8 = 13;

		// Token: 0x04000727 RID: 1831
		internal const int CV_STRING = 14;

		// Token: 0x04000728 RID: 1832
		internal const int CV_PTR = 15;

		// Token: 0x04000729 RID: 1833
		internal const int CV_DATETIME = 16;

		// Token: 0x0400072A RID: 1834
		internal const int CV_TIMESPAN = 17;

		// Token: 0x0400072B RID: 1835
		internal const int CV_OBJECT = 18;

		// Token: 0x0400072C RID: 1836
		internal const int CV_DECIMAL = 19;

		// Token: 0x0400072D RID: 1837
		internal const int CV_ENUM = 21;

		// Token: 0x0400072E RID: 1838
		internal const int CV_MISSING = 22;

		// Token: 0x0400072F RID: 1839
		internal const int CV_NULL = 23;

		// Token: 0x04000730 RID: 1840
		internal const int CV_LAST = 24;

		// Token: 0x04000731 RID: 1841
		internal const int TypeCodeBitMask = 65535;

		// Token: 0x04000732 RID: 1842
		internal const int VTBitMask = -16777216;

		// Token: 0x04000733 RID: 1843
		internal const int VTBitShift = 24;

		// Token: 0x04000734 RID: 1844
		internal const int ArrayBitMask = 65536;

		// Token: 0x04000735 RID: 1845
		internal const int EnumI1 = 1048576;

		// Token: 0x04000736 RID: 1846
		internal const int EnumU1 = 2097152;

		// Token: 0x04000737 RID: 1847
		internal const int EnumI2 = 3145728;

		// Token: 0x04000738 RID: 1848
		internal const int EnumU2 = 4194304;

		// Token: 0x04000739 RID: 1849
		internal const int EnumI4 = 5242880;

		// Token: 0x0400073A RID: 1850
		internal const int EnumU4 = 6291456;

		// Token: 0x0400073B RID: 1851
		internal const int EnumI8 = 7340032;

		// Token: 0x0400073C RID: 1852
		internal const int EnumU8 = 8388608;

		// Token: 0x0400073D RID: 1853
		internal const int EnumMask = 15728640;

		// Token: 0x0400073E RID: 1854
		internal static readonly Type[] ClassTypes = new Type[]
		{
			typeof(Empty),
			typeof(void),
			typeof(bool),
			typeof(char),
			typeof(sbyte),
			typeof(byte),
			typeof(short),
			typeof(ushort),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(ulong),
			typeof(float),
			typeof(double),
			typeof(string),
			typeof(void),
			typeof(DateTime),
			typeof(TimeSpan),
			typeof(object),
			typeof(decimal),
			typeof(object),
			typeof(Missing),
			typeof(DBNull)
		};

		// Token: 0x0400073F RID: 1855
		internal static readonly System.Variant Empty = default(System.Variant);

		// Token: 0x04000740 RID: 1856
		internal static readonly System.Variant Missing = new System.Variant(22, Type.Missing, 0, 0);

		// Token: 0x04000741 RID: 1857
		internal static readonly System.Variant DBNull = new System.Variant(23, System.DBNull.Value, 0, 0);
	}
}
