using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006E3 RID: 1763
	[StructLayout(LayoutKind.Sequential)]
	internal class CategoryMembershipEntry
	{
		// Token: 0x0400233C RID: 9020
		public IDefinitionIdentity Identity;

		// Token: 0x0400233D RID: 9021
		public ISection SubcategoryMembership;
	}
}
