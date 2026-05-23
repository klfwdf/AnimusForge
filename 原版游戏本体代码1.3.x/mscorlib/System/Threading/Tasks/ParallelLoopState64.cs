using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000557 RID: 1367
	internal class ParallelLoopState64 : ParallelLoopState
	{
		// Token: 0x06004057 RID: 16471 RVA: 0x000F0360 File Offset: 0x000EE560
		internal ParallelLoopState64(ParallelLoopStateFlags64 sharedParallelStateFlags)
			: base(sharedParallelStateFlags)
		{
			this.m_sharedParallelStateFlags = sharedParallelStateFlags;
		}

		// Token: 0x1700098A RID: 2442
		// (get) Token: 0x06004058 RID: 16472 RVA: 0x000F0370 File Offset: 0x000EE570
		// (set) Token: 0x06004059 RID: 16473 RVA: 0x000F0378 File Offset: 0x000EE578
		internal long CurrentIteration
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

		// Token: 0x1700098B RID: 2443
		// (get) Token: 0x0600405A RID: 16474 RVA: 0x000F0381 File Offset: 0x000EE581
		internal override bool InternalShouldExitCurrentIteration
		{
			get
			{
				return this.m_sharedParallelStateFlags.ShouldExitLoop(this.CurrentIteration);
			}
		}

		// Token: 0x1700098C RID: 2444
		// (get) Token: 0x0600405B RID: 16475 RVA: 0x000F0394 File Offset: 0x000EE594
		internal override long? InternalLowestBreakIteration
		{
			get
			{
				return this.m_sharedParallelStateFlags.NullableLowestBreakIteration;
			}
		}

		// Token: 0x0600405C RID: 16476 RVA: 0x000F03A1 File Offset: 0x000EE5A1
		internal override void InternalBreak()
		{
			ParallelLoopState.Break(this.CurrentIteration, this.m_sharedParallelStateFlags);
		}

		// Token: 0x04001AE2 RID: 6882
		private ParallelLoopStateFlags64 m_sharedParallelStateFlags;

		// Token: 0x04001AE3 RID: 6883
		private long m_currentIteration;
	}
}
