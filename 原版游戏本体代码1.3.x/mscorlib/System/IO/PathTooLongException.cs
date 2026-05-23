using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.IO
{
	// Token: 0x0200019D RID: 413
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class PathTooLongException : IOException
	{
		// Token: 0x0600195F RID: 6495 RVA: 0x000548D0 File Offset: 0x00052AD0
		[__DynamicallyInvokable]
		public PathTooLongException()
			: base(Environment.GetResourceString("IO.PathTooLong"))
		{
			base.SetErrorCode(-2147024690);
		}

		// Token: 0x06001960 RID: 6496 RVA: 0x000548ED File Offset: 0x00052AED
		[__DynamicallyInvokable]
		public PathTooLongException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147024690);
		}

		// Token: 0x06001961 RID: 6497 RVA: 0x00054901 File Offset: 0x00052B01
		[__DynamicallyInvokable]
		public PathTooLongException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147024690);
		}

		// Token: 0x06001962 RID: 6498 RVA: 0x00054916 File Offset: 0x00052B16
		protected PathTooLongException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
