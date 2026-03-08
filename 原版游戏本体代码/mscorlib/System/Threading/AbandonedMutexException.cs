using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Threading
{
	// Token: 0x020004E4 RID: 1252
	[ComVisible(false)]
	[__DynamicallyInvokable]
	[Serializable]
	public class AbandonedMutexException : SystemException
	{
		// Token: 0x06003B6E RID: 15214 RVA: 0x000E23BB File Offset: 0x000E05BB
		[__DynamicallyInvokable]
		public AbandonedMutexException()
			: base(Environment.GetResourceString("Threading.AbandonedMutexException"))
		{
			base.SetErrorCode(-2146233043);
		}

		// Token: 0x06003B6F RID: 15215 RVA: 0x000E23DF File Offset: 0x000E05DF
		[__DynamicallyInvokable]
		public AbandonedMutexException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233043);
		}

		// Token: 0x06003B70 RID: 15216 RVA: 0x000E23FA File Offset: 0x000E05FA
		[__DynamicallyInvokable]
		public AbandonedMutexException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233043);
		}

		// Token: 0x06003B71 RID: 15217 RVA: 0x000E2416 File Offset: 0x000E0616
		[__DynamicallyInvokable]
		public AbandonedMutexException(int location, WaitHandle handle)
			: base(Environment.GetResourceString("Threading.AbandonedMutexException"))
		{
			base.SetErrorCode(-2146233043);
			this.SetupException(location, handle);
		}

		// Token: 0x06003B72 RID: 15218 RVA: 0x000E2442 File Offset: 0x000E0642
		[__DynamicallyInvokable]
		public AbandonedMutexException(string message, int location, WaitHandle handle)
			: base(message)
		{
			base.SetErrorCode(-2146233043);
			this.SetupException(location, handle);
		}

		// Token: 0x06003B73 RID: 15219 RVA: 0x000E2465 File Offset: 0x000E0665
		[__DynamicallyInvokable]
		public AbandonedMutexException(string message, Exception inner, int location, WaitHandle handle)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233043);
			this.SetupException(location, handle);
		}

		// Token: 0x06003B74 RID: 15220 RVA: 0x000E248A File Offset: 0x000E068A
		private void SetupException(int location, WaitHandle handle)
		{
			this.m_MutexIndex = location;
			if (handle != null)
			{
				this.m_Mutex = handle as Mutex;
			}
		}

		// Token: 0x06003B75 RID: 15221 RVA: 0x000E24A2 File Offset: 0x000E06A2
		protected AbandonedMutexException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x170008FF RID: 2303
		// (get) Token: 0x06003B76 RID: 15222 RVA: 0x000E24B3 File Offset: 0x000E06B3
		[__DynamicallyInvokable]
		public Mutex Mutex
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Mutex;
			}
		}

		// Token: 0x17000900 RID: 2304
		// (get) Token: 0x06003B77 RID: 15223 RVA: 0x000E24BB File Offset: 0x000E06BB
		[__DynamicallyInvokable]
		public int MutexIndex
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_MutexIndex;
			}
		}

		// Token: 0x04001965 RID: 6501
		private int m_MutexIndex = -1;

		// Token: 0x04001966 RID: 6502
		private Mutex m_Mutex;
	}
}
