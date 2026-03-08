using System;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
	// Token: 0x02000329 RID: 809
	[ComVisible(true)]
	[Serializable]
	public enum WindowsAccountType
	{
		// Token: 0x04001028 RID: 4136
		Normal,
		// Token: 0x04001029 RID: 4137
		Guest,
		// Token: 0x0400102A RID: 4138
		System,
		// Token: 0x0400102B RID: 4139
		Anonymous
	}
}
