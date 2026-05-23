using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000611 RID: 1553
	[Flags]
	[ComVisible(true)]
	public enum ExceptionHandlingClauseOptions
	{
		// Token: 0x04001DCB RID: 7627
		Clause = 0,
		// Token: 0x04001DCC RID: 7628
		Filter = 1,
		// Token: 0x04001DCD RID: 7629
		Finally = 2,
		// Token: 0x04001DCE RID: 7630
		Fault = 4
	}
}
