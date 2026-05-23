using System;

namespace System.Threading.Tasks
{
	// Token: 0x0200056C RID: 1388
	internal sealed class ContinuationTaskFromResultTask<TAntecedentResult> : Task
	{
		// Token: 0x0600416A RID: 16746 RVA: 0x000F4424 File Offset: 0x000F2624
		public ContinuationTaskFromResultTask(Task<TAntecedentResult> antecedent, Delegate action, object state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, ref StackCrawlMark stackMark)
			: base(action, state, Task.InternalCurrentIfAttached(creationOptions), default(CancellationToken), creationOptions, internalOptions, null)
		{
			this.m_antecedent = antecedent;
			base.PossiblyCaptureContext(ref stackMark);
		}

		// Token: 0x0600416B RID: 16747 RVA: 0x000F4460 File Offset: 0x000F2660
		internal override void InnerInvoke()
		{
			Task<TAntecedentResult> antecedent = this.m_antecedent;
			this.m_antecedent = null;
			antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();
			Action<Task<TAntecedentResult>> action = this.m_action as Action<Task<TAntecedentResult>>;
			if (action != null)
			{
				action(antecedent);
				return;
			}
			Action<Task<TAntecedentResult>, object> action2 = this.m_action as Action<Task<TAntecedentResult>, object>;
			if (action2 != null)
			{
				action2(antecedent, this.m_stateObject);
				return;
			}
		}

		// Token: 0x04001B55 RID: 6997
		private Task<TAntecedentResult> m_antecedent;
	}
}
