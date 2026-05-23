using System;
using System.Security;

namespace System.Threading.Tasks
{
	// Token: 0x0200055F RID: 1375
	internal sealed class CompletionActionInvoker : IThreadPoolWorkItem
	{
		// Token: 0x06004146 RID: 16710 RVA: 0x000F3DC5 File Offset: 0x000F1FC5
		internal CompletionActionInvoker(ITaskCompletionAction action, Task completingTask)
		{
			this.m_action = action;
			this.m_completingTask = completingTask;
		}

		// Token: 0x06004147 RID: 16711 RVA: 0x000F3DDB File Offset: 0x000F1FDB
		[SecurityCritical]
		public void ExecuteWorkItem()
		{
			this.m_action.Invoke(this.m_completingTask);
		}

		// Token: 0x06004148 RID: 16712 RVA: 0x000F3DEE File Offset: 0x000F1FEE
		[SecurityCritical]
		public void MarkAborted(ThreadAbortException tae)
		{
		}

		// Token: 0x04001B20 RID: 6944
		private readonly ITaskCompletionAction m_action;

		// Token: 0x04001B21 RID: 6945
		private readonly Task m_completingTask;
	}
}
