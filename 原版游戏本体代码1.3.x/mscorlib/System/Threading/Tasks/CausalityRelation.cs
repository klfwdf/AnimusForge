using System;

namespace System.Threading.Tasks
{
	// Token: 0x0200057E RID: 1406
	internal enum CausalityRelation
	{
		// Token: 0x04001B84 RID: 7044
		AssignDelegate,
		// Token: 0x04001B85 RID: 7045
		Join,
		// Token: 0x04001B86 RID: 7046
		Choice,
		// Token: 0x04001B87 RID: 7047
		Cancel,
		// Token: 0x04001B88 RID: 7048
		Error
	}
}
