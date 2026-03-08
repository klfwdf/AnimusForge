using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006E5 RID: 1765
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("97FDCA77-B6F2-4718-A1EB-29D0AECE9C03")]
	[ComImport]
	internal interface ICategoryMembershipEntry
	{
		// Token: 0x17000CF3 RID: 3315
		// (get) Token: 0x0600509D RID: 20637
		CategoryMembershipEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CF4 RID: 3316
		// (get) Token: 0x0600509E RID: 20638
		IDefinitionIdentity Identity
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CF5 RID: 3317
		// (get) Token: 0x0600509F RID: 20639
		ISection SubcategoryMembership
		{
			[SecurityCritical]
			get;
		}
	}
}
