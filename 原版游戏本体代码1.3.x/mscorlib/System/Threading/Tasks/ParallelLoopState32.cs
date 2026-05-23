using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000556 RID: 1366
	internal class ParallelLoopState32 : ParallelLoopState
	{
		// Token: 0x06004051 RID: 16465 RVA: 0x000F030C File Offset: 0x000EE50C
		internal ParallelLoopState32(ParallelLoopStateFlags32 sharedParallelStateFlags)
			: base(sharedParallelStateFlags)
		{
			this.m_sharedParallelStateFlags = sharedParallelStateFlags;
		}

		// Token: 0x17000987 RID: 2439
		// (get) Token: 0x06004052 RID: 16466 RVA: 0x000F031C File Offset: 0x000EE51C
		// (set) Token: 0x06004053 RID: 16467 RVA: 0x000F0324 File Offset: 0x000EE524
		internal int CurrentIteration
		{
			get
			{
				return this.m_currentIteration;
			}
			set
			{
				this.m_currentIteration = value;
			}
		}

		// Token: 0x17000988 RID: 2440
		// (get) Token: 0x06004054 RID: 16468 RVA: 0x000F032D File Offset: 0x000EE52D
		internal override bool InternalShouldExitCurrentIteration
		{
			get
			{
				return this.m_sharedParallelStateFlags.ShouldExitLoop(this.CurrentIteration);
			}
		}

		// Token: 0x17000989 RID: 2441
		// (get) Token: 0x06004055 RID: 16469 RVA: 0x000F0340 File Offset: 0x000EE540
		internal override long? InternalLowestBreakIteration
		{
			get
			{
				return this.m_sharedParallelStateFlags.NullableLowestBreakIteration;
			}
		}

		// Token: 0x06004056 RID: 16470 RVA: 0x000F034D File Offset: 0x000EE54D
		internal override void InternalBreak()
		{
			ParallelLoopState.Break(this.CurrentIteration, this.m_sharedParallelStateFlags);
		}

		// Token: 0x04001AE0 RID: 6880
		private ParallelLoopStateFlags32 m_sharedParallelStateFlags;

		// Token: 0x04001AE1 RID: 6881
		private int m_currentIteration;
	}
}
