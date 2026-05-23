using System;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000952 RID: 2386
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class MarshalDirectiveException : SystemException
	{
		// Token: 0x060061AD RID: 25005 RVA: 0x0014E5E8 File Offset: 0x0014C7E8
		[__DynamicallyInvokable]
		public MarshalDirectiveException()
			: base(Environment.GetResourceString("Arg_MarshalDirectiveException"))
		{
			base.SetErrorCode(-2146233035);
		}

		// Token: 0x060061AE RID: 25006 RVA: 0x0014E605 File Offset: 0x0014C805
		[__DynamicallyInvokable]
		public MarshalDirectiveException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233035);
		}

		// Token: 0x060061AF RID: 25007 RVA: 0x0014E619 File Offset: 0x0014C819
		[__DynamicallyInvokable]
		public MarshalDirectiveException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233035);
		}

		// Token: 0x060061B0 RID: 25008 RVA: 0x0014E62E File Offset: 0x0014C82E
		protected MarshalDirectiveException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
