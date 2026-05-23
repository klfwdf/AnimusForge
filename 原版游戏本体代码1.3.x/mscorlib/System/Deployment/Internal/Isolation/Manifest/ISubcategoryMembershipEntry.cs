using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006E2 RID: 1762
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("5A7A54D7-5AD5-418e-AB7A-CF823A8D48D0")]
	[ComImport]
	internal interface ISubcategoryMembershipEntry
	{
		// Token: 0x17000CF0 RID: 3312
		// (get) Token: 0x06005099 RID: 20633
		SubcategoryMembershipEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CF1 RID: 3313
		// (get) Token: 0x0600509A RID: 20634
		string Subcategory
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000CF2 RID: 3314
		// (get) Token: 0x0600509B RID: 20635
		ISection CategoryMembershipData
		{
			[SecurityCritical]
			get;
		}
	}
}
