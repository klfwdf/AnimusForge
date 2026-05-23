using System;

namespace System.Threading.Tasks
{
	// Token: 0x0200056A RID: 1386
	internal sealed class ContinuationTaskFromTask : Task
	{
		// Token: 0x06004166 RID: 16742 RVA: 0x000F42F0 File Offset: 0x000F24F0
		public ContinuationTaskFromTask(Task antecedent, Delegate action, object state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, ref StackCrawlMark stackMark)
			: base(action, state, Task.InternalCurrentIfAttached(creationOptions), default(CancellationToken), creationOptions, internalOptions, null)
		{
			this.m_antecedent = antecedent;
			base.PossiblyCaptureContext(ref stackMark);
		}

		// Token: 0x06004167 RID: 16743 RVA: 0x000F432C File Offset: 0x000F252C
		internal override void InnerInvoke()
		{
			Task antecedent = this.m_antecedent;
			this.m_antecedent = null;
			antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();
			Action<Task> action = this.m_action as Action<Task>;
			if (action != null)
			{
				action(antecedent);
				return;
			}
			Action<Task, object> action2 = this.m_action as Action<Task, object>;
			if (action2 != null)
			{
				action2(antecedent, this.m_stateObject);
				return;
			}
		}

		// Token: 0x04001B53 RID: 6995
		private Task m_antecedent;
	}
}
