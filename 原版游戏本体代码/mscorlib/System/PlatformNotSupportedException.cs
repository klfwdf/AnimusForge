using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x02000123 RID: 291
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class PlatformNotSupportedException : NotSupportedException
	{
		// Token: 0x060010EC RID: 4332 RVA: 0x00032E37 File Offset: 0x00031037
		[__DynamicallyInvokable]
		public PlatformNotSupportedException()
			: base(Environment.GetResourceString("Arg_PlatformNotSupported"))
		{
			base.SetErrorCode(-2146233031);
		}

		// Token: 0x060010ED RID: 4333 RVA: 0x00032E54 File Offset: 0x00031054
		[__DynamicallyInvokable]
		public PlatformNotSupportedException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233031);
		}

		// Token: 0x060010EE RID: 4334 RVA: 0x00032E68 File Offset: 0x00031068
		[__DynamicallyInvokable]
		public PlatformNotSupportedException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233031);
		}

		// Token: 0x060010EF RID: 4335 RVA: 0x00032E7D File Offset: 0x0003107D
		protected PlatformNotSupportedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
