using System;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200094D RID: 2381
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class InvalidOleVariantTypeException : SystemException
	{
		// Token: 0x060060BE RID: 24766 RVA: 0x0014CB45 File Offset: 0x0014AD45
		[__DynamicallyInvokable]
		public InvalidOleVariantTypeException()
			: base(Environment.GetResourceString("Arg_InvalidOleVariantTypeException"))
		{
			base.SetErrorCode(-2146233039);
		}

		// Token: 0x060060BF RID: 24767 RVA: 0x0014CB62 File Offset: 0x0014AD62
		[__DynamicallyInvokable]
		public InvalidOleVariantTypeException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233039);
		}

		// Token: 0x060060C0 RID: 24768 RVA: 0x0014CB76 File Offset: 0x0014AD76
		[__DynamicallyInvokable]
		public InvalidOleVariantTypeException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233039);
		}

		// Token: 0x060060C1 RID: 24769 RVA: 0x0014CB8B File Offset: 0x0014AD8B
		protected InvalidOleVariantTypeException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
