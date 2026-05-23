using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006E0 RID: 1760
	[StructLayout(LayoutKind.Sequential)]
	internal class SubcategoryMembershipEntry
	{
		// Token: 0x04002338 RID: 9016
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Subcategory;

		// Token: 0x04002339 RID: 9017
		public ISection CategoryMembershipData;
	}
}
