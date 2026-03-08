using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000514 RID: 1300
	internal class ThreadHelper
	{
		// Token: 0x06003D1D RID: 15645 RVA: 0x000E6209 File Offset: 0x000E4409
		internal ThreadHelper(Delegate start)
		{
			this._start = start;
		}

		// Token: 0x06003D1E RID: 15646 RVA: 0x000E6218 File Offset: 0x000E4418
		internal void SetExecutionContextHelper(ExecutionContext ec)
		{
			this._executionContext = ec;
		}

		// Token: 0x06003D1F RID: 15647 RVA: 0x000E6224 File Offset: 0x000E4424
		[SecurityCritical]
		private static void ThreadStart_Context(object state)
		{
			ThreadHelper threadHelper = (ThreadHelper)state;
			if (threadHelper._start is ThreadStart)
			{
				((ThreadStart)threadHelper._start)();
				return;
			}
			((ParameterizedThreadStart)threadHelper._start)(threadHelper._startArg);
		}

		// Token: 0x06003D20 RID: 15648 RVA: 0x000E626C File Offset: 0x000E446C
		[SecurityCritical]
		internal void ThreadStart(object obj)
		{
			this._startArg = obj;
			if (this._executionContext != null)
			{
				ExecutionContext.Run(this._executionContext, ThreadHelper._ccb, this);
				return;
			}
			((ParameterizedThreadStart)this._start)(obj);
		}

		// Token: 0x06003D21 RID: 15649 RVA: 0x000E62A0 File Offset: 0x000E44A0
		[SecurityCritical]
		internal void ThreadStart()
		{
			if (this._executionContext != null)
			{
				ExecutionContext.Run(this._executionContext, ThreadHelper._ccb, this);
				return;
			}
			((ThreadStart)this._start)();
		}

		// Token: 0x040019E4 RID: 6628
		private Delegate _start;

		// Token: 0x040019E5 RID: 6629
		private object _startArg;

		// Token: 0x040019E6 RID: 6630
		private ExecutionContext _executionContext;

		// Token: 0x040019E7 RID: 6631
		[SecurityCritical]
		internal static ContextCallback _ccb = new ContextCallback(ThreadHelper.ThreadStart_Context);
	}
}
