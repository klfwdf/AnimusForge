using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000603 RID: 1539
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum MemberTypes
	{
		// Token: 0x04001D5F RID: 7519
		Constructor = 1,
		// Token: 0x04001D60 RID: 7520
		Event = 2,
		// Token: 0x04001D61 RID: 7521
		Field = 4,
		// Token: 0x04001D62 RID: 7522
		Method = 8,
		// Token: 0x04001D63 RID: 7523
		Property = 16,
		// Token: 0x04001D64 RID: 7524
		TypeInfo = 32,
		// Token: 0x04001D65 RID: 7525
		Custom = 64,
		// Token: 0x04001D66 RID: 7526
		NestedType = 128,
		// Token: 0x04001D67 RID: 7527
		All = 191
	}
}
