using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000522 RID: 1314
	internal interface IThreadPoolWorkItem
	{
		// Token: 0x06003DE4 RID: 15844
		[SecurityCritical]
		void ExecuteWorkItem();

		// Token: 0x06003DE5 RID: 15845
		[SecurityCritical]
		void MarkAborted(ThreadAbortException tae);
	}
}
