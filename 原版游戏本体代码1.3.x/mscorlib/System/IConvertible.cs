using System;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000103 RID: 259
	[CLSCompliant(false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface IConvertible
	{
		// Token: 0x06000FBF RID: 4031
		[__DynamicallyInvokable]
		TypeCode GetTypeCode();

		// Token: 0x06000FC0 RID: 4032
		[__DynamicallyInvokable]
		bool ToBoolean(IFormatProvider provider);

		// Token: 0x06000FC1 RID: 4033
		[__DynamicallyInvokable]
		char ToChar(IFormatProvider provider);

		// Token: 0x06000FC2 RID: 4034
		[__DynamicallyInvokable]
		sbyte ToSByte(IFormatProvider provider);

		// Token: 0x06000FC3 RID: 4035
		[__DynamicallyInvokable]
		byte ToByte(IFormatProvider provider);

		// Token: 0x06000FC4 RID: 4036
		[__DynamicallyInvokable]
		short ToInt16(IFormatProvider provider);

		// Token: 0x06000FC5 RID: 4037
		[__DynamicallyInvokable]
		ushort ToUInt16(IFormatProvider provider);

		// Token: 0x06000FC6 RID: 4038
		[__DynamicallyInvokable]
		int ToInt32(IFormatProvider provider);

		// Token: 0x06000FC7 RID: 4039
		[__DynamicallyInvokable]
		uint ToUInt32(IFormatProvider provider);

		// Token: 0x06000FC8 RID: 4040
		[__DynamicallyInvokable]
		long ToInt64(IFormatProvider provider);

		// Token: 0x06000FC9 RID: 4041
		[__DynamicallyInvokable]
		ulong ToUInt64(IFormatProvider provider);

		// Token: 0x06000FCA RID: 4042
		[__DynamicallyInvokable]
		float ToSingle(IFormatProvider provider);

		// Token: 0x06000FCB RID: 4043
		[__DynamicallyInvokable]
		double ToDouble(IFormatProvider provider);

		// Token: 0x06000FCC RID: 4044
		[__DynamicallyInvokable]
		decimal ToDecimal(IFormatProvider provider);

		// Token: 0x06000FCD RID: 4045
		[__DynamicallyInvokable]
		DateTime ToDateTime(IFormatProvider provider);

		// Token: 0x06000FCE RID: 4046
		[__DynamicallyInvokable]
		string ToString(IFormatProvider provider);

		// Token: 0x06000FCF RID: 4047
		[__DynamicallyInvokable]
		object ToType(Type conversionType, IFormatProvider provider);
	}
}
