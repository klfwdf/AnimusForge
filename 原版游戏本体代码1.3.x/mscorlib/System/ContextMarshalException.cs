using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000CA RID: 202
	[ComVisible(true)]
	[Serializable]
	public class ContextMarshalException : SystemException
	{
		// Token: 0x06000BA5 RID: 2981 RVA: 0x000250BF File Offset: 0x000232BF
		public ContextMarshalException()
			: base(Environment.GetResourceString("Arg_ContextMarshalException"))
		{
			base.SetErrorCode(-2146233084);
		}

		// Token: 0x06000BA6 RID: 2982 RVA: 0x000250DC File Offset: 0x000232DC
		public ContextMarshalException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233084);
		}

		// Token: 0x06000BA7 RID: 2983 RVA: 0x000250F0 File Offset: 0x000232F0
		public ContextMarshalException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233084);
		}

		// Token: 0x06000BA8 RID: 2984 RVA: 0x00025105 File Offset: 0x00023305
		protected ContextMarshalException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
