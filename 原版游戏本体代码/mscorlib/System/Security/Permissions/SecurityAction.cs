using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002EE RID: 750
	[ComVisible(true)]
	[Serializable]
	public enum SecurityAction
	{
		// Token: 0x04000EDE RID: 3806
		Demand = 2,
		// Token: 0x04000EDF RID: 3807
		Assert,
		// Token: 0x04000EE0 RID: 3808
		[Obsolete("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		Deny,
		// Token: 0x04000EE1 RID: 3809
		PermitOnly,
		// Token: 0x04000EE2 RID: 3810
		LinkDemand,
		// Token: 0x04000EE3 RID: 3811
		InheritanceDemand,
		// Token: 0x04000EE4 RID: 3812
		[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		RequestMinimum,
		// Token: 0x04000EE5 RID: 3813
		[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		RequestOptional,
		// Token: 0x04000EE6 RID: 3814
		[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		RequestRefuse
	}
}
