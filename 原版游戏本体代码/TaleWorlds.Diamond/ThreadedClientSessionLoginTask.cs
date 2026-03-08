using System;
using System.Threading.Tasks;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200002F RID: 47
	internal sealed class ThreadedClientSessionLoginTask : ThreadedClientSessionTask
	{
		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000FB RID: 251 RVA: 0x00003B01 File Offset: 0x00001D01
		// (set) Token: 0x060000FC RID: 252 RVA: 0x00003B09 File Offset: 0x00001D09
		public LoginResult LoginResult { get; private set; }

		// Token: 0x060000FD RID: 253 RVA: 0x00003B12 File Offset: 0x00001D12
		public ThreadedClientSessionLoginTask(IClientSession session, LoginMessage message)
			: base(session)
		{
			this._message = message;
			this._taskCompletionSource = new TaskCompletionSource<bool>();
		}

		// Token: 0x060000FE RID: 254 RVA: 0x00003B2D File Offset: 0x00001D2D
		public override void BeginJob()
		{
			this._task = this.Login();
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00003B3C File Offset: 0x00001D3C
		private async Task Login()
		{
			LoginResult loginResult = await base.Session.Login(this._message);
			this.LoginResult = loginResult;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x00003B81 File Offset: 0x00001D81
		public override void DoMainThreadJob()
		{
			if (this._task.IsCompleted)
			{
				this._taskCompletionSource.SetResult(true);
				base.Finished = true;
			}
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00003BA4 File Offset: 0x00001DA4
		public async Task Wait()
		{
			await this._taskCompletionSource.Task;
		}

		// Token: 0x0400004D RID: 77
		private TaskCompletionSource<bool> _taskCompletionSource;

		// Token: 0x0400004E RID: 78
		private LoginMessage _message;

		// Token: 0x04000050 RID: 80
		private Task _task;
	}
}
