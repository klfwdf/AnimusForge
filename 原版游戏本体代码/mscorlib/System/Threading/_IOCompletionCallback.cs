using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000504 RID: 1284
	internal class _IOCompletionCallback
	{
		// Token: 0x06003C8D RID: 15501 RVA: 0x000E4906 File Offset: 0x000E2B06
		[SecurityCritical]
		internal _IOCompletionCallback(IOCompletionCallback ioCompletionCallback, ref StackCrawlMark stackMark)
		{
			this._ioCompletionCallback = ioCompletionCallback;
			this._executionContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
		}

		// Token: 0x06003C8E RID: 15502 RVA: 0x000E4924 File Offset: 0x000E2B24
		[SecurityCritical]
		internal static void IOCompletionCallback_Context(object state)
		{
			_IOCompletionCallback iocompletionCallback = (_IOCompletionCallback)state;
			iocompletionCallback._ioCompletionCallback(iocompletionCallback._errorCode, iocompletionCallback._numBytes, iocompletionCallback._pOVERLAP);
		}

		// Token: 0x06003C8F RID: 15503 RVA: 0x000E4958 File Offset: 0x000E2B58
		[SecurityCritical]
		internal unsafe static void PerformIOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pOVERLAP)
		{
			do
			{
				Overlapped overlapped = OverlappedData.GetOverlappedFromNative(pOVERLAP).m_overlapped;
				_IOCompletionCallback iocbHelper = overlapped.iocbHelper;
				if (iocbHelper == null || iocbHelper._executionContext == null || iocbHelper._executionContext.IsDefaultFTContext(true))
				{
					IOCompletionCallback userCallback = overlapped.UserCallback;
					userCallback(errorCode, numBytes, pOVERLAP);
				}
				else
				{
					iocbHelper._errorCode = errorCode;
					iocbHelper._numBytes = numBytes;
					iocbHelper._pOVERLAP = pOVERLAP;
					using (ExecutionContext executionContext = iocbHelper._executionContext.CreateCopy())
					{
						ExecutionContext.Run(executionContext, _IOCompletionCallback._ccb, iocbHelper, true);
					}
				}
				OverlappedData.CheckVMForIOPacket(out pOVERLAP, out errorCode, out numBytes);
			}
			while (pOVERLAP != null);
		}

		// Token: 0x040019AA RID: 6570
		[SecurityCritical]
		private IOCompletionCallback _ioCompletionCallback;

		// Token: 0x040019AB RID: 6571
		private ExecutionContext _executionContext;

		// Token: 0x040019AC RID: 6572
		private uint _errorCode;

		// Token: 0x040019AD RID: 6573
		private uint _numBytes;

		// Token: 0x040019AE RID: 6574
		[SecurityCritical]
		private unsafe NativeOverlapped* _pOVERLAP;

		// Token: 0x040019AF RID: 6575
		internal static ContextCallback _ccb = new ContextCallback(_IOCompletionCallback.IOCompletionCallback_Context);
	}
}
