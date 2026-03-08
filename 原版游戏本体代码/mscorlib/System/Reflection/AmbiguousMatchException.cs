using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
	// Token: 0x020005AF RID: 1455
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class AmbiguousMatchException : SystemException
	{
		// Token: 0x0600436B RID: 17259 RVA: 0x000FA801 File Offset: 0x000F8A01
		[__DynamicallyInvokable]
		public AmbiguousMatchException()
			: base(Environment.GetResourceString("RFLCT.Ambiguous"))
		{
			base.SetErrorCode(-2147475171);
		}

		// Token: 0x0600436C RID: 17260 RVA: 0x000FA81E File Offset: 0x000F8A1E
		[__DynamicallyInvokable]
		public AmbiguousMatchException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147475171);
		}

		// Token: 0x0600436D RID: 17261 RVA: 0x000FA832 File Offset: 0x000F8A32
		[__DynamicallyInvokable]
		public AmbiguousMatchException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2147475171);
		}

		// Token: 0x0600436E RID: 17262 RVA: 0x000FA847 File Offset: 0x000F8A47
		internal AmbiguousMatchException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
