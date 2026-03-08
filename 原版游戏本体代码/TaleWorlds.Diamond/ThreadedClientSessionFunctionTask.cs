using System;
using System.Threading.Tasks;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000031 RID: 49
	internal sealed class ThreadedClientSessionFunctionTask : ThreadedClientSessionTask
	{
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x06000107 RID: 263 RVA: 0x00003C26 File Offset: 0x00001E26
		// (set) Token: 0x06000108 RID: 264 RVA: 0x00003C2E File Offset: 0x00001E2E
		public FunctionResult FunctionResult { get; private set; }

		// Token: 0x06000109 RID: 265 RVA: 0x00003C37 File Offset: 0x00001E37
		public ThreadedClientSessionFunctionTask(IClientSession session, Message message)
			: base(session)
		{
			this._message = message;
			this._taskCompletionSource = new TaskCompletionSource<bool>();
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00003C52 File Offset: 0x00001E52
		public override void BeginJob()
		{
			this._task = this.CallFunction();
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00003C60 File Offset: 0x00001E60
		private async Task CallFunction()
		{
			FunctionResult functionResult = await base.Session.CallFunction<FunctionResult>(this._message);
			this.FunctionResult = functionResult;
		}

		// Token: 0x0600010C RID: 268 RVA: 0x00003CA5 File Offset: 0x00001EA5
		public override void DoMainThreadJob()
		{
			if (this._task.IsCompleted)
			{
				this._taskCompletionSource.SetResult(true);
				base.Finished = true;
			}
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00003CC8 File Offset: 0x00001EC8
		public async Task Wait()
		{
			await this._taskCompletionSource.Task;
		}

		// Token: 0x04000052 RID: 82
		private TaskCompletionSource<bool> _taskCompletionSource;

		// Token: 0x04000053 RID: 83
		private Message _message;

		// Token: 0x04000055 RID: 85
		private Task _task;
	}
}
