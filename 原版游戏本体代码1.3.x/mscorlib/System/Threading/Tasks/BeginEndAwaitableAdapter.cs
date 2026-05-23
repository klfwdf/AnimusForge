using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Threading.Tasks
{
	// Token: 0x02000588 RID: 1416
	internal sealed class BeginEndAwaitableAdapter : ICriticalNotifyCompletion, INotifyCompletion
	{
		// Token: 0x0600429C RID: 17052 RVA: 0x000F849A File Offset: 0x000F669A
		public BeginEndAwaitableAdapter GetAwaiter()
		{
			return this;
		}

		// Token: 0x170009E5 RID: 2533
		// (get) Token: 0x0600429D RID: 17053 RVA: 0x000F849D File Offset: 0x000F669D
		public bool IsCompleted
		{
			get
			{
				return this._continuation == BeginEndAwaitableAdapter.CALLBACK_RAN;
			}
		}

		// Token: 0x0600429E RID: 17054 RVA: 0x000F84AF File Offset: 0x000F66AF
		[SecurityCritical]
		public void UnsafeOnCompleted(Action continuation)
		{
			this.OnCompleted(continuation);
		}

		// Token: 0x0600429F RID: 17055 RVA: 0x000F84B8 File Offset: 0x000F66B8
		public void OnCompleted(Action continuation)
		{
			if (this._continuation == BeginEndAwaitableAdapter.CALLBACK_RAN || Interlocked.CompareExchange<Action>(ref this._continuation, continuation, null) == BeginEndAwaitableAdapter.CALLBACK_RAN)
			{
				Task.Run(continuation);
			}
		}

		// Token: 0x060042A0 RID: 17056 RVA: 0x000F84EC File Offset: 0x000F66EC
		public IAsyncResult GetResult()
		{
			IAsyncResult asyncResult = this._asyncResult;
			this._asyncResult = null;
			this._continuation = null;
			return asyncResult;
		}

		// Token: 0x04001BB9 RID: 7097
		private static readonly Action CALLBACK_RAN = delegate()
		{
		};

		// Token: 0x04001BBA RID: 7098
		private IAsyncResult _asyncResult;

		// Token: 0x04001BBB RID: 7099
		private Action _continuation;

		// Token: 0x04001BBC RID: 7100
		public static readonly AsyncCallback Callback = delegate(IAsyncResult asyncResult)
		{
			BeginEndAwaitableAdapter beginEndAwaitableAdapter = (BeginEndAwaitableAdapter)asyncResult.AsyncState;
			beginEndAwaitableAdapter._asyncResult = asyncResult;
			Action action = Interlocked.Exchange<Action>(ref beginEndAwaitableAdapter._continuation, BeginEndAwaitableAdapter.CALLBACK_RAN);
			if (action != null)
			{
				action();
			}
		};
	}
}
