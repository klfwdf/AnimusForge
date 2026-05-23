using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x0200073F RID: 1855
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class SerializationException : SystemException
	{
		// Token: 0x060051D4 RID: 20948 RVA: 0x0011FEAB File Offset: 0x0011E0AB
		[__DynamicallyInvokable]
		public SerializationException()
			: base(SerializationException._nullMessage)
		{
			base.SetErrorCode(-2146233076);
		}

		// Token: 0x060051D5 RID: 20949 RVA: 0x0011FEC3 File Offset: 0x0011E0C3
		[__DynamicallyInvokable]
		public SerializationException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233076);
		}

		// Token: 0x060051D6 RID: 20950 RVA: 0x0011FED7 File Offset: 0x0011E0D7
		[__DynamicallyInvokable]
		public SerializationException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233076);
		}

		// Token: 0x060051D7 RID: 20951 RVA: 0x0011FEEC File Offset: 0x0011E0EC
		protected SerializationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x04002445 RID: 9285
		private static string _nullMessage = Environment.GetResourceString("Arg_SerializationException");
	}
}
