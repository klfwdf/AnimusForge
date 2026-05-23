using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200091F RID: 2335
	[Obsolete("The IDispatchImplAttribute is deprecated.", false)]
	[ComVisible(true)]
	[Serializable]
	public enum IDispatchImplType
	{
		// Token: 0x04002A78 RID: 10872
		SystemDefinedImpl,
		// Token: 0x04002A79 RID: 10873
		InternalImpl,
		// Token: 0x04002A7A RID: 10874
		CompatibleImpl
	}
}
