using System;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.Diagnostics.Contracts
{
	// Token: 0x02000417 RID: 1047
	[__DynamicallyInvokable]
	public sealed class ContractFailedEventArgs : EventArgs
	{
		// Token: 0x0600341C RID: 13340 RVA: 0x000C6C4C File Offset: 0x000C4E4C
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public ContractFailedEventArgs(ContractFailureKind failureKind, string message, string condition, Exception originalException)
		{
			this._failureKind = failureKind;
			this._message = message;
			this._condition = condition;
			this._originalException = originalException;
		}

		// Token: 0x170007A9 RID: 1961
		// (get) Token: 0x0600341D RID: 13341 RVA: 0x000C6C71 File Offset: 0x000C4E71
		[__DynamicallyInvokable]
		public string Message
		{
			[__DynamicallyInvokable]
			get
			{
				return this._message;
			}
		}

		// Token: 0x170007AA RID: 1962
		// (get) Token: 0x0600341E RID: 13342 RVA: 0x000C6C79 File Offset: 0x000C4E79
		[__DynamicallyInvokable]
		public string Condition
		{
			[__DynamicallyInvokable]
			get
			{
				return this._condition;
			}
		}

		// Token: 0x170007AB RID: 1963
		// (get) Token: 0x0600341F RID: 13343 RVA: 0x000C6C81 File Offset: 0x000C4E81
		[__DynamicallyInvokable]
		public ContractFailureKind FailureKind
		{
			[__DynamicallyInvokable]
			get
			{
				return this._failureKind;
			}
		}

		// Token: 0x170007AC RID: 1964
		// (get) Token: 0x06003420 RID: 13344 RVA: 0x000C6C89 File Offset: 0x000C4E89
		[__DynamicallyInvokable]
		public Exception OriginalException
		{
			[__DynamicallyInvokable]
			get
			{
				return this._originalException;
			}
		}

		// Token: 0x170007AD RID: 1965
		// (get) Token: 0x06003421 RID: 13345 RVA: 0x000C6C91 File Offset: 0x000C4E91
		[__DynamicallyInvokable]
		public bool Handled
		{
			[__DynamicallyInvokable]
			get
			{
				return this._handled;
			}
		}

		// Token: 0x06003422 RID: 13346 RVA: 0x000C6C99 File Offset: 0x000C4E99
		[SecurityCritical]
		[__DynamicallyInvokable]
		public void SetHandled()
		{
			this._handled = true;
		}

		// Token: 0x170007AE RID: 1966
		// (get) Token: 0x06003423 RID: 13347 RVA: 0x000C6CA2 File Offset: 0x000C4EA2
		[__DynamicallyInvokable]
		public bool Unwind
		{
			[__DynamicallyInvokable]
			get
			{
				return this._unwind;
			}
		}

		// Token: 0x06003424 RID: 13348 RVA: 0x000C6CAA File Offset: 0x000C4EAA
		[SecurityCritical]
		[__DynamicallyInvokable]
		public void SetUnwind()
		{
			this._unwind = true;
		}

		// Token: 0x04001718 RID: 5912
		private ContractFailureKind _failureKind;

		// Token: 0x04001719 RID: 5913
		private string _message;

		// Token: 0x0400171A RID: 5914
		private string _condition;

		// Token: 0x0400171B RID: 5915
		private Exception _originalException;

		// Token: 0x0400171C RID: 5916
		private bool _handled;

		// Token: 0x0400171D RID: 5917
		private bool _unwind;

		// Token: 0x0400171E RID: 5918
		internal Exception thrownDuringHandler;
	}
}
