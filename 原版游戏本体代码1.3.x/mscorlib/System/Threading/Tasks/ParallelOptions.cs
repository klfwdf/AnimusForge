using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000550 RID: 1360
	[__DynamicallyInvokable]
	public class ParallelOptions
	{
		// Token: 0x0600400D RID: 16397 RVA: 0x000EE319 File Offset: 0x000EC519
		[__DynamicallyInvokable]
		public ParallelOptions()
		{
			this.m_scheduler = TaskScheduler.Default;
			this.m_maxDegreeOfParallelism = -1;
			this.m_cancellationToken = CancellationToken.None;
		}

		// Token: 0x1700097C RID: 2428
		// (get) Token: 0x0600400E RID: 16398 RVA: 0x000EE33E File Offset: 0x000EC53E
		// (set) Token: 0x0600400F RID: 16399 RVA: 0x000EE346 File Offset: 0x000EC546
		[__DynamicallyInvokable]
		public TaskScheduler TaskScheduler
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_scheduler;
			}
			[__DynamicallyInvokable]
			set
			{
				this.m_scheduler = value;
			}
		}

		// Token: 0x1700097D RID: 2429
		// (get) Token: 0x06004010 RID: 16400 RVA: 0x000EE34F File Offset: 0x000EC54F
		internal TaskScheduler EffectiveTaskScheduler
		{
			get
			{
				if (this.m_scheduler == null)
				{
					return TaskScheduler.Current;
				}
				return this.m_scheduler;
			}
		}

		// Token: 0x1700097E RID: 2430
		// (get) Token: 0x06004011 RID: 16401 RVA: 0x000EE365 File Offset: 0x000EC565
		// (set) Token: 0x06004012 RID: 16402 RVA: 0x000EE36D File Offset: 0x000EC56D
		[__DynamicallyInvokable]
		public int MaxDegreeOfParallelism
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_maxDegreeOfParallelism;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value == 0 || value < -1)
				{
					throw new ArgumentOutOfRangeException("MaxDegreeOfParallelism");
				}
				this.m_maxDegreeOfParallelism = value;
			}
		}

		// Token: 0x1700097F RID: 2431
		// (get) Token: 0x06004013 RID: 16403 RVA: 0x000EE388 File Offset: 0x000EC588
		// (set) Token: 0x06004014 RID: 16404 RVA: 0x000EE390 File Offset: 0x000EC590
		[__DynamicallyInvokable]
		public CancellationToken CancellationToken
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_cancellationToken;
			}
			[__DynamicallyInvokable]
			set
			{
				this.m_cancellationToken = value;
			}
		}

		// Token: 0x17000980 RID: 2432
		// (get) Token: 0x06004015 RID: 16405 RVA: 0x000EE39C File Offset: 0x000EC59C
		internal int EffectiveMaxConcurrencyLevel
		{
			get
			{
				int num = this.MaxDegreeOfParallelism;
				int maximumConcurrencyLevel = this.EffectiveTaskScheduler.MaximumConcurrencyLevel;
				if (maximumConcurrencyLevel > 0 && maximumConcurrencyLevel != 2147483647)
				{
					num = ((num == -1) ? maximumConcurrencyLevel : Math.Min(maximumConcurrencyLevel, num));
				}
				return num;
			}
		}

		// Token: 0x04001ACB RID: 6859
		private TaskScheduler m_scheduler;

		// Token: 0x04001ACC RID: 6860
		private int m_maxDegreeOfParallelism;

		// Token: 0x04001ACD RID: 6861
		private CancellationToken m_cancellationToken;
	}
}
