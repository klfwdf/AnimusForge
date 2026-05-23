using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000568 RID: 1384
	internal interface ITaskCompletionAction
	{
		// Token: 0x0600415E RID: 16734
		void Invoke(Task completingTask);
	}
}
