using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000E2 RID: 226
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class FieldAccessException : MemberAccessException
	{
		// Token: 0x06000E7F RID: 3711 RVA: 0x0002CC67 File Offset: 0x0002AE67
		[__DynamicallyInvokable]
		public FieldAccessException()
			: base(Environment.GetResourceString("Arg_FieldAccessException"))
		{
			base.SetErrorCode(-2146233081);
		}

		// Token: 0x06000E80 RID: 3712 RVA: 0x0002CC84 File Offset: 0x0002AE84
		[__DynamicallyInvokable]
		public FieldAccessException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233081);
		}

		// Token: 0x06000E81 RID: 3713 RVA: 0x0002CC98 File Offset: 0x0002AE98
		[__DynamicallyInvokable]
		public FieldAccessException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233081);
		}

		// Token: 0x06000E82 RID: 3714 RVA: 0x0002CCAD File Offset: 0x0002AEAD
		protected FieldAccessException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
