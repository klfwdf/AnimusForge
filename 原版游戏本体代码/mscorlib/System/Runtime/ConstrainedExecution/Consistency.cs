using System;

namespace System.Runtime.ConstrainedExecution
{
	// Token: 0x0200072A RID: 1834
	[Serializable]
	public enum Consistency
	{
		// Token: 0x04002431 RID: 9265
		MayCorruptProcess,
		// Token: 0x04002432 RID: 9266
		MayCorruptAppDomain,
		// Token: 0x04002433 RID: 9267
		MayCorruptInstance,
		// Token: 0x04002434 RID: 9268
		WillNotCorruptState
	}
}
