using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Principal;

namespace System.Threading
{
	// Token: 0x020004F6 RID: 1270
	internal struct ExecutionContextSwitcher
	{
		// Token: 0x06003BEB RID: 15339 RVA: 0x000E3278 File Offset: 0x000E1478
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[HandleProcessCorruptedStateExceptions]
		internal bool UndoNoThrow()
		{
			try
			{
				this.Undo();
			}
			catch (Exception exception)
			{
				if (!AppContextSwitches.UseLegacyExecutionContextBehaviorUponUndoFailure)
				{
					Environment.FailFast(Environment.GetResourceString("ExecutionContext_UndoFailed"), exception);
				}
				return false;
			}
			return true;
		}

		// Token: 0x06003BEC RID: 15340 RVA: 0x000E32BC File Offset: 0x000E14BC
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal void Undo()
		{
			if (this.thread == null)
			{
				return;
			}
			Thread thread = this.thread;
			if (this.hecsw != null)
			{
				HostExecutionContextSwitcher.Undo(this.hecsw);
			}
			ExecutionContext.Reader executionContextReader = thread.GetExecutionContextReader();
			thread.SetExecutionContext(this.outerEC, this.outerECBelongsToScope);
			if (this.scsw.currSC != null)
			{
				this.scsw.Undo();
			}
			if (this.wiIsValid)
			{
				SecurityContext.RestoreCurrentWI(this.outerEC, executionContextReader, this.wi, this.cachedAlwaysFlowImpersonationPolicy);
			}
			this.thread = null;
			ExecutionContext.OnAsyncLocalContextChanged(executionContextReader.DangerousGetRawExecutionContext(), this.outerEC.DangerousGetRawExecutionContext());
		}

		// Token: 0x04001983 RID: 6531
		internal ExecutionContext.Reader outerEC;

		// Token: 0x04001984 RID: 6532
		internal bool outerECBelongsToScope;

		// Token: 0x04001985 RID: 6533
		internal SecurityContextSwitcher scsw;

		// Token: 0x04001986 RID: 6534
		internal object hecsw;

		// Token: 0x04001987 RID: 6535
		internal WindowsIdentity wi;

		// Token: 0x04001988 RID: 6536
		internal bool cachedAlwaysFlowImpersonationPolicy;

		// Token: 0x04001989 RID: 6537
		internal bool wiIsValid;

		// Token: 0x0400198A RID: 6538
		internal Thread thread;
	}
}
