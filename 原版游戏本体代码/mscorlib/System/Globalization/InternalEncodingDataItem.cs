using System;
using System.Security;

namespace System.Globalization
{
	// Token: 0x020003B9 RID: 953
	internal struct InternalEncodingDataItem
	{
		// Token: 0x0400141C RID: 5148
		[SecurityCritical]
		internal unsafe sbyte* webName;

		// Token: 0x0400141D RID: 5149
		internal ushort codePage;
	}
}
