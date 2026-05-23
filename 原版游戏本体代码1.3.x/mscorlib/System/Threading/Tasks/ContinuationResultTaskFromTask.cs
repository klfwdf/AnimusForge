using System;

namespace System.Threading.Tasks
{
	// Token: 0x0200056B RID: 1387
	internal sealed class ContinuationResultTaskFromTask<TResult> : Task<TResult>
	{
		// Token: 0x06004168 RID: 16744 RVA: 0x000F4384 File Offset: 0x000F2584
		public ContinuationResultTaskFromTask(Task antecedent, Delegate function, object state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, ref StackCrawlMark stackMark)
			: base(function, state, Task.InternalCurrentIfAttached(creationOptions), default(CancellationToken), creationOptions, internalOptions, null)
		{
			this.m_antecedent = antecedent;
			base.PossiblyCaptureContext(ref stackMark);
		}

		// Token: 0x06004169 RID: 16745 RVA: 0x000F43C0 File Offset: 0x000F25C0
		internal override void InnerInvoke()
		{
			Task antecedent = this.m_antecedent;
			this.m_antecedent = null;
			antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();
			Func<Task, TResult> func = this.m_action as Func<Task, TResult>;
			if (func != null)
			{
				this.m_result = func(antecedent);
				return;
			}
			Func<Task, object, TResult> func2 = this.m_action as Func<Task, object, TResult>;
			if (func2 != null)
			{
				this.m_result = func2(antecedent, this.m_stateObject);
				return;
			}
		}

		// Token: 0x04001B54 RID: 6996
		private Task m_antecedent;
	}
}
