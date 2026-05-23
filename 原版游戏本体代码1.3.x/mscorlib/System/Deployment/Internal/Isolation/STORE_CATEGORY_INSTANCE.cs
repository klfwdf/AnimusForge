using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x0200067C RID: 1660
	internal struct STORE_CATEGORY_INSTANCE
	{
		// Token: 0x040021F6 RID: 8694
		public IDefinitionAppId DefinitionAppId_Application;

		// Token: 0x040021F7 RID: 8695
		[MarshalAs(UnmanagedType.LPWStr)]
		public string XMLSnippet;
	}
}
