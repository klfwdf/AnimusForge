using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x0200062E RID: 1582
	[ComVisible(true)]
	[Flags]
	[Serializable]
	public enum AssemblyBuilderAccess
	{
		// Token: 0x04001E7E RID: 7806
		Run = 1,
		// Token: 0x04001E7F RID: 7807
		Save = 2,
		// Token: 0x04001E80 RID: 7808
		RunAndSave = 3,
		// Token: 0x04001E81 RID: 7809
		ReflectionOnly = 6,
		// Token: 0x04001E82 RID: 7810
		RunAndCollect = 9
	}
}
