using System;
using System.Security;

namespace System.Globalization
{
	// Token: 0x020003BA RID: 954
	internal struct InternalCodePageDataItem
	{
		// Token: 0x0400141E RID: 5150
		internal ushort codePage;

		// Token: 0x0400141F RID: 5151
		internal ushort uiFamilyCodePage;

		// Token: 0x04001420 RID: 5152
		internal uint flags;

		// Token: 0x04001421 RID: 5153
		[SecurityCritical]
		internal unsafe sbyte* Names;
	}
}
