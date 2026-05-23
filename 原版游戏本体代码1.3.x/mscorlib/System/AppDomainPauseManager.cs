using System;
using System.Security;
using System.Threading;

namespace System
{
	// Token: 0x0200009C RID: 156
	[SecurityCritical]
	internal class AppDomainPauseManager
	{
		// Token: 0x0600089D RID: 2205 RVA: 0x0001D658 File Offset: 0x0001B858
		[SecurityCritical]
		public AppDomainPauseManager()
		{
			AppDomainPauseManager.isPaused = false;
		}

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x0600089F RID: 2207 RVA: 0x0001D674 File Offset: 0x0001B874
		internal static AppDomainPauseManager Instance
		{
			[SecurityCritical]
			get
			{
				return AppDomainPauseManager.instance;
			}
		}

		// Token: 0x060008A0 RID: 2208 RVA: 0x0001D67B File Offset: 0x0001B87B
		[SecurityCritical]
		public void Pausing()
		{
		}

		// Token: 0x060008A1 RID: 2209 RVA: 0x0001D67D File Offset: 0x0001B87D
		[SecurityCritical]
		public void Paused()
		{
			if (AppDomainPauseManager.ResumeEvent == null)
			{
				AppDomainPauseManager.ResumeEvent = new ManualResetEvent(false);
			}
			else
			{
				AppDomainPauseManager.ResumeEvent.Reset();
			}
			Timer.Pause();
			AppDomainPauseManager.isPaused = true;
		}

		// Token: 0x060008A2 RID: 2210 RVA: 0x0001D6AB File Offset: 0x0001B8AB
		[SecurityCritical]
		public void Resuming()
		{
			AppDomainPauseManager.isPaused = false;
			AppDomainPauseManager.ResumeEvent.Set();
		}

		// Token: 0x060008A3 RID: 2211 RVA: 0x0001D6C0 File Offset: 0x0001B8C0
		[SecurityCritical]
		public void Resumed()
		{
			Timer.Resume();
		}

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x060008A4 RID: 2212 RVA: 0x0001D6C7 File Offset: 0x0001B8C7
		internal static bool IsPaused
		{
			[SecurityCritical]
			get
			{
				return AppDomainPauseManager.isPaused;
			}
		}

		// Token: 0x170000ED RID: 237
		// (get) Token: 0x060008A5 RID: 2213 RVA: 0x0001D6D0 File Offset: 0x0001B8D0
		// (set) Token: 0x060008A6 RID: 2214 RVA: 0x0001D6D7 File Offset: 0x0001B8D7
		internal static ManualResetEvent ResumeEvent
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x040003A3 RID: 931
		private static readonly AppDomainPauseManager instance = new AppDomainPauseManager();

		// Token: 0x040003A4 RID: 932
		private static volatile bool isPaused;
	}
}
