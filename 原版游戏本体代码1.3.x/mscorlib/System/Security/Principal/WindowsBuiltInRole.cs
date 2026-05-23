using System;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
	// Token: 0x02000332 RID: 818
	[ComVisible(true)]
	[Serializable]
	public enum WindowsBuiltInRole
	{
		// Token: 0x04001084 RID: 4228
		Administrator = 544,
		// Token: 0x04001085 RID: 4229
		User,
		// Token: 0x04001086 RID: 4230
		Guest,
		// Token: 0x04001087 RID: 4231
		PowerUser,
		// Token: 0x04001088 RID: 4232
		AccountOperator,
		// Token: 0x04001089 RID: 4233
		SystemOperator,
		// Token: 0x0400108A RID: 4234
		PrintOperator,
		// Token: 0x0400108B RID: 4235
		BackupOperator,
		// Token: 0x0400108C RID: 4236
		Replicator
	}
}
