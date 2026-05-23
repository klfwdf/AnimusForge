using System;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Proxies
{
	// Token: 0x02000803 RID: 2051
	internal class AgileAsyncWorkerItem
	{
		// Token: 0x0600586B RID: 22635 RVA: 0x00137F17 File Offset: 0x00136117
		[SecurityCritical]
		public AgileAsyncWorkerItem(IMethodCallMessage message, AsyncResult ar, object target)
		{
			this._message = new MethodCall(message);
			this._ar = ar;
			this._target = target;
		}

		// Token: 0x0600586C RID: 22636 RVA: 0x00137F39 File Offset: 0x00136139
		[SecurityCritical]
		public static void ThreadPoolCallBack(object o)
		{
			((AgileAsyncWorkerItem)o).DoAsyncCall();
		}

		// Token: 0x0600586D RID: 22637 RVA: 0x00137F46 File Offset: 0x00136146
		[SecurityCritical]
		public void DoAsyncCall()
		{
			new StackBuilderSink(this._target).AsyncProcessMessage(this._message, this._ar);
		}

		// Token: 0x0400284F RID: 10319
		private IMethodCallMessage _message;

		// Token: 0x04002850 RID: 10320
		private AsyncResult _ar;

		// Token: 0x04002851 RID: 10321
		private object _target;
	}
}
