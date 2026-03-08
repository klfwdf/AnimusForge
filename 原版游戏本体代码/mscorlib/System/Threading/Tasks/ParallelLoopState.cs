using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.Threading.Tasks
{
	// Token: 0x02000555 RID: 1365
	[DebuggerDisplay("ShouldExitCurrentIteration = {ShouldExitCurrentIteration}")]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public class ParallelLoopState
	{
		// Token: 0x06004045 RID: 16453 RVA: 0x000F016C File Offset: 0x000EE36C
		internal ParallelLoopState(ParallelLoopStateFlags fbase)
		{
			this.m_flagsBase = fbase;
		}

		// Token: 0x17000981 RID: 2433
		// (get) Token: 0x06004046 RID: 16454 RVA: 0x000F017B File Offset: 0x000EE37B
		internal virtual bool InternalShouldExitCurrentIteration
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod"));
			}
		}

		// Token: 0x17000982 RID: 2434
		// (get) Token: 0x06004047 RID: 16455 RVA: 0x000F018C File Offset: 0x000EE38C
		[__DynamicallyInvokable]
		public bool ShouldExitCurrentIteration
		{
			[__DynamicallyInvokable]
			get
			{
				return this.InternalShouldExitCurrentIteration;
			}
		}

		// Token: 0x17000983 RID: 2435
		// (get) Token: 0x06004048 RID: 16456 RVA: 0x000F0194 File Offset: 0x000EE394
		[__DynamicallyInvokable]
		public bool IsStopped
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.m_flagsBase.LoopStateFlags & ParallelLoopStateFlags.PLS_STOPPED) != 0;
			}
		}

		// Token: 0x17000984 RID: 2436
		// (get) Token: 0x06004049 RID: 16457 RVA: 0x000F01AA File Offset: 0x000EE3AA
		[__DynamicallyInvokable]
		public bool IsExceptional
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.m_flagsBase.LoopStateFlags & ParallelLoopStateFlags.PLS_EXCEPTIONAL) != 0;
			}
		}

		// Token: 0x17000985 RID: 2437
		// (get) Token: 0x0600404A RID: 16458 RVA: 0x000F01C0 File Offset: 0x000EE3C0
		internal virtual long? InternalLowestBreakIteration
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod"));
			}
		}

		// Token: 0x17000986 RID: 2438
		// (get) Token: 0x0600404B RID: 16459 RVA: 0x000F01D1 File Offset: 0x000EE3D1
		[__DynamicallyInvokable]
		public long? LowestBreakIteration
		{
			[__DynamicallyInvokable]
			get
			{
				return this.InternalLowestBreakIteration;
			}
		}

		// Token: 0x0600404C RID: 16460 RVA: 0x000F01D9 File Offset: 0x000EE3D9
		[__DynamicallyInvokable]
		public void Stop()
		{
			this.m_flagsBase.Stop();
		}

		// Token: 0x0600404D RID: 16461 RVA: 0x000F01E6 File Offset: 0x000EE3E6
		internal virtual void InternalBreak()
		{
			throw new NotSupportedException(Environment.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod"));
		}

		// Token: 0x0600404E RID: 16462 RVA: 0x000F01F7 File Offset: 0x000EE3F7
		[__DynamicallyInvokable]
		public void Break()
		{
			this.InternalBreak();
		}

		// Token: 0x0600404F RID: 16463 RVA: 0x000F0200 File Offset: 0x000EE400
		internal static void Break(int iteration, ParallelLoopStateFlags32 pflags)
		{
			int pls_NONE = ParallelLoopStateFlags.PLS_NONE;
			if (pflags.AtomicLoopStateUpdate(ParallelLoopStateFlags.PLS_BROKEN, ParallelLoopStateFlags.PLS_STOPPED | ParallelLoopStateFlags.PLS_EXCEPTIONAL | ParallelLoopStateFlags.PLS_CANCELED, ref pls_NONE))
			{
				int lowestBreakIteration = pflags.m_lowestBreakIteration;
				if (iteration < lowestBreakIteration)
				{
					SpinWait spinWait = default(SpinWait);
					while (Interlocked.CompareExchange(ref pflags.m_lowestBreakIteration, iteration, lowestBreakIteration) != lowestBreakIteration)
					{
						spinWait.SpinOnce();
						lowestBreakIteration = pflags.m_lowestBreakIteration;
						if (iteration > lowestBreakIteration)
						{
							break;
						}
					}
				}
				return;
			}
			if ((pls_NONE & ParallelLoopStateFlags.PLS_STOPPED) != 0)
			{
				throw new InvalidOperationException(Environment.GetResourceString("ParallelState_Break_InvalidOperationException_BreakAfterStop"));
			}
		}

		// Token: 0x06004050 RID: 16464 RVA: 0x000F0288 File Offset: 0x000EE488
		internal static void Break(long iteration, ParallelLoopStateFlags64 pflags)
		{
			int pls_NONE = ParallelLoopStateFlags.PLS_NONE;
			if (pflags.AtomicLoopStateUpdate(ParallelLoopStateFlags.PLS_BROKEN, ParallelLoopStateFlags.PLS_STOPPED | ParallelLoopStateFlags.PLS_EXCEPTIONAL | ParallelLoopStateFlags.PLS_CANCELED, ref pls_NONE))
			{
				long lowestBreakIteration = pflags.LowestBreakIteration;
				if (iteration < lowestBreakIteration)
				{
					SpinWait spinWait = default(SpinWait);
					while (Interlocked.CompareExchange(ref pflags.m_lowestBreakIteration, iteration, lowestBreakIteration) != lowestBreakIteration)
					{
						spinWait.SpinOnce();
						lowestBreakIteration = pflags.LowestBreakIteration;
						if (iteration > lowestBreakIteration)
						{
							break;
						}
					}
				}
				return;
			}
			if ((pls_NONE & ParallelLoopStateFlags.PLS_STOPPED) != 0)
			{
				throw new InvalidOperationException(Environment.GetResourceString("ParallelState_Break_InvalidOperationException_BreakAfterStop"));
			}
		}

		// Token: 0x04001ADF RID: 6879
		private ParallelLoopStateFlags m_flagsBase;
	}
}
