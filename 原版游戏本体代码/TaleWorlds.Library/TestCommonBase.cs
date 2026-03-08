using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaleWorlds.Library
{
	// Token: 0x02000093 RID: 147
	public abstract class TestCommonBase
	{
		// Token: 0x0600053A RID: 1338
		public abstract void Tick();

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x0600053B RID: 1339 RVA: 0x0001298B File Offset: 0x00010B8B
		public static TestCommonBase BaseInstance
		{
			get
			{
				return TestCommonBase._baseInstance;
			}
		}

		// Token: 0x0600053C RID: 1340 RVA: 0x00012992 File Offset: 0x00010B92
		public void StartTimeoutTimer()
		{
			this.timeoutTimerStart = DateTime.Now;
		}

		// Token: 0x0600053D RID: 1341 RVA: 0x0001299F File Offset: 0x00010B9F
		public void ToggleTimeoutTimer()
		{
			this.timeoutTimerEnabled = !this.timeoutTimerEnabled;
		}

		// Token: 0x0600053E RID: 1342 RVA: 0x000129B0 File Offset: 0x00010BB0
		public bool CheckTimeoutTimer()
		{
			return this.timeoutTimerEnabled && DateTime.Now.Subtract(this.timeoutTimerStart).TotalSeconds > (double)this.commonWaitTimeoutLimits;
		}

		// Token: 0x0600053F RID: 1343 RVA: 0x000129EE File Offset: 0x00010BEE
		protected TestCommonBase()
		{
			TestCommonBase._baseInstance = this;
		}

		// Token: 0x06000540 RID: 1344 RVA: 0x00012A24 File Offset: 0x00010C24
		public virtual string GetGameStatus()
		{
			return "";
		}

		// Token: 0x06000541 RID: 1345 RVA: 0x00012A2C File Offset: 0x00010C2C
		public void WaitFor(double seconds)
		{
			if (!this.isParallelThread)
			{
				DateTime now = DateTime.Now;
				while ((DateTime.Now - now).TotalSeconds < seconds)
				{
					Monitor.Pulse(this.TestLock);
					Monitor.Wait(this.TestLock);
				}
			}
		}

		// Token: 0x06000542 RID: 1346 RVA: 0x00012A78 File Offset: 0x00010C78
		public virtual async Task WaitUntil(Func<bool> func)
		{
			while (!func())
			{
				await this.WaitForAsync(0.1);
			}
		}

		// Token: 0x06000543 RID: 1347 RVA: 0x00012AC5 File Offset: 0x00010CC5
		public Task WaitForAsync(double seconds, Random random)
		{
			return Task.Delay((int)(seconds * 1000.0 * random.NextDouble()));
		}

		// Token: 0x06000544 RID: 1348 RVA: 0x00012ADF File Offset: 0x00010CDF
		public Task WaitForAsync(double seconds)
		{
			return Task.Delay((int)(seconds * 1000.0));
		}

		// Token: 0x06000545 RID: 1349 RVA: 0x00012AF2 File Offset: 0x00010CF2
		public static string GetAttachmentsFolderPath()
		{
			return "..\\..\\..\\Tools\\TestAutomation\\Attachments\\";
		}

		// Token: 0x06000546 RID: 1350 RVA: 0x00012AF9 File Offset: 0x00010CF9
		public virtual void OnFinalize()
		{
			TestCommonBase._baseInstance = null;
		}

		// Token: 0x0400019A RID: 410
		public int TestRandomSeed;

		// Token: 0x0400019B RID: 411
		public bool IsTestEnabled;

		// Token: 0x0400019C RID: 412
		public bool isParallelThread;

		// Token: 0x0400019D RID: 413
		public string SceneNameToOpenOnStartup;

		// Token: 0x0400019E RID: 414
		public object TestLock = new object();

		// Token: 0x0400019F RID: 415
		private static TestCommonBase _baseInstance;

		// Token: 0x040001A0 RID: 416
		private DateTime timeoutTimerStart = DateTime.Now;

		// Token: 0x040001A1 RID: 417
		private bool timeoutTimerEnabled = true;

		// Token: 0x040001A2 RID: 418
		private int commonWaitTimeoutLimits = 1140;
	}
}
