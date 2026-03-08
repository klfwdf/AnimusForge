using System;

namespace System.Threading.Tasks
{
	// Token: 0x02000579 RID: 1401
	[__DynamicallyInvokable]
	public class UnobservedTaskExceptionEventArgs : EventArgs
	{
		// Token: 0x06004220 RID: 16928 RVA: 0x000F65CB File Offset: 0x000F47CB
		[__DynamicallyInvokable]
		public UnobservedTaskExceptionEventArgs(AggregateException exception)
		{
			this.m_exception = exception;
		}

		// Token: 0x06004221 RID: 16929 RVA: 0x000F65DA File Offset: 0x000F47DA
		[__DynamicallyInvokable]
		public void SetObserved()
		{
			this.m_observed = true;
		}

		// Token: 0x170009D0 RID: 2512
		// (get) Token: 0x06004222 RID: 16930 RVA: 0x000F65E3 File Offset: 0x000F47E3
		[__DynamicallyInvokable]
		public bool Observed
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_observed;
			}
		}

		// Token: 0x170009D1 RID: 2513
		// (get) Token: 0x06004223 RID: 16931 RVA: 0x000F65EB File Offset: 0x000F47EB
		[__DynamicallyInvokable]
		public AggregateException Exception
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_exception;
			}
		}

		// Token: 0x04001B76 RID: 7030
		private AggregateException m_exception;

		// Token: 0x04001B77 RID: 7031
		internal bool m_observed;
	}
}
