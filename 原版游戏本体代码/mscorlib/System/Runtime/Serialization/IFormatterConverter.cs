using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x02000733 RID: 1843
	[CLSCompliant(false)]
	[ComVisible(true)]
	public interface IFormatterConverter
	{
		// Token: 0x060051AF RID: 20911
		object Convert(object value, Type type);

		// Token: 0x060051B0 RID: 20912
		object Convert(object value, TypeCode typeCode);

		// Token: 0x060051B1 RID: 20913
		bool ToBoolean(object value);

		// Token: 0x060051B2 RID: 20914
		char ToChar(object value);

		// Token: 0x060051B3 RID: 20915
		sbyte ToSByte(object value);

		// Token: 0x060051B4 RID: 20916
		byte ToByte(object value);

		// Token: 0x060051B5 RID: 20917
		short ToInt16(object value);

		// Token: 0x060051B6 RID: 20918
		ushort ToUInt16(object value);

		// Token: 0x060051B7 RID: 20919
		int ToInt32(object value);

		// Token: 0x060051B8 RID: 20920
		uint ToUInt32(object value);

		// Token: 0x060051B9 RID: 20921
		long ToInt64(object value);

		// Token: 0x060051BA RID: 20922
		ulong ToUInt64(object value);

		// Token: 0x060051BB RID: 20923
		float ToSingle(object value);

		// Token: 0x060051BC RID: 20924
		double ToDouble(object value);

		// Token: 0x060051BD RID: 20925
		decimal ToDecimal(object value);

		// Token: 0x060051BE RID: 20926
		DateTime ToDateTime(object value);

		// Token: 0x060051BF RID: 20927
		string ToString(object value);
	}
}
