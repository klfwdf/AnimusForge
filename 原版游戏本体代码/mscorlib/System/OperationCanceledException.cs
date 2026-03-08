using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System
{
	// Token: 0x0200011D RID: 285
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class OperationCanceledException : SystemException
	{
		// Token: 0x170001C6 RID: 454
		// (get) Token: 0x060010CE RID: 4302 RVA: 0x00032BEE File Offset: 0x00030DEE
		// (set) Token: 0x060010CF RID: 4303 RVA: 0x00032BF6 File Offset: 0x00030DF6
		[__DynamicallyInvokable]
		public CancellationToken CancellationToken
		{
			[__DynamicallyInvokable]
			get
			{
				return this._cancellationToken;
			}
			private set
			{
				this._cancellationToken = value;
			}
		}

		// Token: 0x060010D0 RID: 4304 RVA: 0x00032BFF File Offset: 0x00030DFF
		[__DynamicallyInvokable]
		public OperationCanceledException()
			: base(Environment.GetResourceString("OperationCanceled"))
		{
			base.SetErrorCode(-2146233029);
		}

		// Token: 0x060010D1 RID: 4305 RVA: 0x00032C1C File Offset: 0x00030E1C
		[__DynamicallyInvokable]
		public OperationCanceledException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233029);
		}

		// Token: 0x060010D2 RID: 4306 RVA: 0x00032C30 File Offset: 0x00030E30
		[__DynamicallyInvokable]
		public OperationCanceledException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233029);
		}

		// Token: 0x060010D3 RID: 4307 RVA: 0x00032C45 File Offset: 0x00030E45
		[__DynamicallyInvokable]
		public OperationCanceledException(CancellationToken token)
			: this()
		{
			this.CancellationToken = token;
		}

		// Token: 0x060010D4 RID: 4308 RVA: 0x00032C54 File Offset: 0x00030E54
		[__DynamicallyInvokable]
		public OperationCanceledException(string message, CancellationToken token)
			: this(message)
		{
			this.CancellationToken = token;
		}

		// Token: 0x060010D5 RID: 4309 RVA: 0x00032C64 File Offset: 0x00030E64
		[__DynamicallyInvokable]
		public OperationCanceledException(string message, Exception innerException, CancellationToken token)
			: this(message, innerException)
		{
			this.CancellationToken = token;
		}

		// Token: 0x060010D6 RID: 4310 RVA: 0x00032C75 File Offset: 0x00030E75
		protected OperationCanceledException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x040005D3 RID: 1491
		[NonSerialized]
		private CancellationToken _cancellationToken;
	}
}
