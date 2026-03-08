using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x0200072E RID: 1838
	[ComVisible(true)]
	public class FormatterConverter : IFormatterConverter
	{
		// Token: 0x06005179 RID: 20857 RVA: 0x0011F21E File Offset: 0x0011D41E
		public object Convert(object value, Type type)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
		}

		// Token: 0x0600517A RID: 20858 RVA: 0x0011F23A File Offset: 0x0011D43A
		public object Convert(object value, TypeCode typeCode)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ChangeType(value, typeCode, CultureInfo.InvariantCulture);
		}

		// Token: 0x0600517B RID: 20859 RVA: 0x0011F256 File Offset: 0x0011D456
		public bool ToBoolean(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x0600517C RID: 20860 RVA: 0x0011F271 File Offset: 0x0011D471
		public char ToChar(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToChar(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x0600517D RID: 20861 RVA: 0x0011F28C File Offset: 0x0011D48C
		[CLSCompliant(false)]
		public sbyte ToSByte(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToSByte(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x0600517E RID: 20862 RVA: 0x0011F2A7 File Offset: 0x0011D4A7
		public byte ToByte(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToByte(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x0600517F RID: 20863 RVA: 0x0011F2C2 File Offset: 0x0011D4C2
		public short ToInt16(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToInt16(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06005180 RID: 20864 RVA: 0x0011F2DD File Offset: 0x0011D4DD
		[CLSCompliant(false)]
		public ushort ToUInt16(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToUInt16(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06005181 RID: 20865 RVA: 0x0011F2F8 File Offset: 0x0011D4F8
		public int ToInt32(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06005182 RID: 20866 RVA: 0x0011F313 File Offset: 0x0011D513
		[CLSCompliant(false)]
		public uint ToUInt32(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToUInt32(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06005183 RID: 20867 RVA: 0x0011F32E File Offset: 0x0011D52E
		public long ToInt64(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToInt64(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06005184 RID: 20868 RVA: 0x0011F349 File Offset: 0x0011D549
		[CLSCompliant(false)]
		public ulong ToUInt64(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToUInt64(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06005185 RID: 20869 RVA: 0x0011F364 File Offset: 0x0011D564
		public float ToSingle(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToSingle(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06005186 RID: 20870 RVA: 0x0011F37F File Offset: 0x0011D57F
		public double ToDouble(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06005187 RID: 20871 RVA: 0x0011F39A File Offset: 0x0011D59A
		public decimal ToDecimal(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06005188 RID: 20872 RVA: 0x0011F3B5 File Offset: 0x0011D5B5
		public DateTime ToDateTime(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToDateTime(value, CultureInfo.InvariantCulture);
		}

		// Token: 0x06005189 RID: 20873 RVA: 0x0011F3D0 File Offset: 0x0011D5D0
		public string ToString(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return System.Convert.ToString(value, CultureInfo.InvariantCulture);
		}
	}
}
