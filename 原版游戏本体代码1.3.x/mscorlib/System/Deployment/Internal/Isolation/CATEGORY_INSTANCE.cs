using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x0200067F RID: 1663
	internal struct CATEGORY_INSTANCE
	{
		// Token: 0x040021FA RID: 8698
		public IDefinitionAppId DefinitionAppId_Application;

		// Token: 0x040021FB RID: 8699
		[MarshalAs(UnmanagedType.LPWStr)]
		public string XMLSnippet;
	}
}
