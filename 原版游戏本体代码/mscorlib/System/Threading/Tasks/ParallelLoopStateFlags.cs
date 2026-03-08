using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000558 RID: 1368
	internal class ParallelLoopStateFlags
	{
		// Token: 0x1700098D RID: 2445
		// (get) Token: 0x0600405D RID: 16477 RVA: 0x000F03B4 File Offset: 0x000EE5B4
		internal int LoopStateFlags
		{
			get
			{
				return this.m_LoopStateFlags;
			}
		}

		// Token: 0x0600405E RID: 16478 RVA: 0x000F03C0 File Offset: 0x000EE5C0
		internal bool AtomicLoopStateUpdate(int newState, int illegalStates)
		{
			int num = 0;
			return this.AtomicLoopStateUpdate(newState, illegalStates, ref num);
		}

		// Token: 0x0600405F RID: 16479 RVA: 0x000F03DC File Offset: 0x000EE5DC
		internal bool AtomicLoopStateUpdate(int newState, int illegalStates, ref int oldState)
		{
			SpinWait spinWait = default(SpinWait);
			for (;;)
			{
				oldState = this.m_LoopStateFlags;
				if ((oldState & illegalStates) != 0)
				{
					break;
				}
				if (Interlocked.CompareExchange(ref this.m_LoopStateFlags, oldState | newState, oldState) == oldState)
				{
					return true;
				}
				spinWait.SpinOnce();
			}
			return false;
		}

		// Token: 0x06004060 RID: 16480 RVA: 0x000F0422 File Offset: 0x000EE622
		internal void SetExceptional()
		{
			this.AtomicLoopStateUpdate(ParallelLoopStateFlags.PLS_EXCEPTIONAL, ParallelLoopStateFlags.PLS_NONE);
		}

		// Token: 0x06004061 RID: 16481 RVA: 0x000F0435 File Offset: 0x000EE635
		internal void Stop()
		{
			if (!this.AtomicLoopStateUpdate(ParallelLoopStateFlags.PLS_STOPPED, ParallelLoopStateFlags.PLS_BROKEN))
			{
				throw new InvalidOperationException(Environment.GetResourceString("ParallelState_Stop_InvalidOperationException_StopAfterBreak"));
			}
		}

		// Token: 0x06004062 RID: 16482 RVA: 0x000F0459 File Offset: 0x000EE659
		internal bool Cancel()
		{
			return this.AtomicLoopStateUpdate(ParallelLoopStateFlags.PLS_CANCELED, ParallelLoopStateFlags.PLS_NONE);
		}

		// Token: 0x04001AE4 RID: 6884
		internal static int PLS_NONE;

		// Token: 0x04001AE5 RID: 6885
		internal static int PLS_EXCEPTIONAL = 1;

		// Token: 0x04001AE6 RID: 6886
		internal static int PLS_BROKEN = 2;

		// Token: 0x04001AE7 RID: 6887
		internal static int PLS_STOPPED = 4;

		// Token: 0x04001AE8 RID: 6888
		internal static int PLS_CANCELED = 8;

		// Token: 0x04001AE9 RID: 6889
		private volatile int m_LoopStateFlags = ParallelLoopStateFlags.PLS_NONE;
	}
}
