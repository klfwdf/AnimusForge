using System;
using System.Runtime.InteropServices;

namespace System.Configuration.Assemblies
{
	// Token: 0x02000173 RID: 371
	[ComVisible(true)]
	[Serializable]
	public enum AssemblyVersionCompatibility
	{
		// Token: 0x040007E7 RID: 2023
		SameMachine = 1,
		// Token: 0x040007E8 RID: 2024
		SameProcess,
		// Token: 0x040007E9 RID: 2025
		SameDomain
	}
}
