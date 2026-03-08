using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000559 RID: 1369
	internal class ParallelLoopStateFlags32 : ParallelLoopStateFlags
	{
		// Token: 0x1700098E RID: 2446
		// (get) Token: 0x06004065 RID: 16485 RVA: 0x000F049A File Offset: 0x000EE69A
		internal int LowestBreakIteration
		{
			get
			{
				return this.m_lowestBreakIteration;
			}
		}

		// Token: 0x1700098F RID: 2447
		// (get) Token: 0x06004066 RID: 16486 RVA: 0x000F04A4 File Offset: 0x000EE6A4
		internal long? NullableLowestBreakIteration
		{
			get
			{
				if (this.m_lowestBreakIteration == 2147483647)
				{
					return null;
				}
				long value = (long)this.m_lowestBreakIteration;
				if (IntPtr.Size >= 8)
				{
					return new long?(value);
				}
				return new long?(Interlocked.Read(ref value));
			}
		}

		// Token: 0x06004067 RID: 16487 RVA: 0x000F04F0 File Offset: 0x000EE6F0
		internal bool ShouldExitLoop(int CallerIteration)
		{
			int loopStateFlags = base.LoopStateFlags;
			return loopStateFlags != ParallelLoopStateFlags.PLS_NONE && ((loopStateFlags & (ParallelLoopStateFlags.PLS_EXCEPTIONAL | ParallelLoopStateFlags.PLS_STOPPED | ParallelLoopStateFlags.PLS_CANCELED)) != 0 || ((loopStateFlags & ParallelLoopStateFlags.PLS_BROKEN) != 0 && CallerIteration > this.LowestBreakIteration));
		}

		// Token: 0x06004068 RID: 16488 RVA: 0x000F053C File Offset: 0x000EE73C
		internal bool ShouldExitLoop()
		{
			int loopStateFlags = base.LoopStateFlags;
			return loopStateFlags != ParallelLoopStateFlags.PLS_NONE && (loopStateFlags & (ParallelLoopStateFlags.PLS_EXCEPTIONAL | ParallelLoopStateFlags.PLS_CANCELED)) != 0;
		}

		// Token: 0x04001AEA RID: 6890
		internal volatile int m_lowestBreakIteration = int.MaxValue;
	}
}
