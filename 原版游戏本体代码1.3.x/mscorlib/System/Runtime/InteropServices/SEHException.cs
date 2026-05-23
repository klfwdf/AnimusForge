using System;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000955 RID: 2389
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class SEHException : ExternalException
	{
		// Token: 0x060061BE RID: 25022 RVA: 0x0014E720 File Offset: 0x0014C920
		[__DynamicallyInvokable]
		public SEHException()
		{
			base.SetErrorCode(-2147467259);
		}

		// Token: 0x060061BF RID: 25023 RVA: 0x0014E733 File Offset: 0x0014C933
		[__DynamicallyInvokable]
		public SEHException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147467259);
		}

		// Token: 0x060061C0 RID: 25024 RVA: 0x0014E747 File Offset: 0x0014C947
		[__DynamicallyInvokable]
		public SEHException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2147467259);
		}

		// Token: 0x060061C1 RID: 25025 RVA: 0x0014E75C File Offset: 0x0014C95C
		protected SEHException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x060061C2 RID: 25026 RVA: 0x0014E766 File Offset: 0x0014C966
		[__DynamicallyInvokable]
		public virtual bool CanResume()
		{
			return false;
		}
	}
}
