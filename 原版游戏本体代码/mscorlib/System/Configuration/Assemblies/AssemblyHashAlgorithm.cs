using System;
using System.Runtime.InteropServices;

namespace System.Configuration.Assemblies
{
	// Token: 0x02000172 RID: 370
	[ComVisible(true)]
	[Serializable]
	public enum AssemblyHashAlgorithm
	{
		// Token: 0x040007E0 RID: 2016
		None,
		// Token: 0x040007E1 RID: 2017
		MD5 = 32771,
		// Token: 0x040007E2 RID: 2018
		SHA1,
		// Token: 0x040007E3 RID: 2019
		[ComVisible(false)]
		SHA256 = 32780,
		// Token: 0x040007E4 RID: 2020
		[ComVisible(false)]
		SHA384,
		// Token: 0x040007E5 RID: 2021
		[ComVisible(false)]
		SHA512
	}
}
