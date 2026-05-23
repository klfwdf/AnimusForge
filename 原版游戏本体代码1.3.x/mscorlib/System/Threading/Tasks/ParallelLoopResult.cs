using System;

namespace System.Threading.Tasks
{
	// Token: 0x0200055B RID: 1371
	[__DynamicallyInvokable]
	public struct ParallelLoopResult
	{
		// Token: 0x17000992 RID: 2450
		// (get) Token: 0x0600406F RID: 16495 RVA: 0x000F0679 File Offset: 0x000EE879
		[__DynamicallyInvokable]
		public bool IsCompleted
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_completed;
			}
		}

		// Token: 0x17000993 RID: 2451
		// (get) Token: 0x06004070 RID: 16496 RVA: 0x000F0681 File Offset: 0x000EE881
		[__DynamicallyInvokable]
		public long? LowestBreakIteration
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_lowestBreakIteration;
			}
		}

		// Token: 0x04001AEC RID: 6892
		internal bool m_completed;

		// Token: 0x04001AED RID: 6893
		internal long? m_lowestBreakIteration;
	}
}
