using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x02000081 RID: 129
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class OutOfMemoryException : SystemException
	{
		// Token: 0x060006C8 RID: 1736 RVA: 0x000178FA File Offset: 0x00015AFA
		[__DynamicallyInvokable]
		public OutOfMemoryException()
			: base(Exception.GetMessageFromNativeResources(Exception.ExceptionMessageKind.OutOfMemory))
		{
			base.SetErrorCode(-2147024882);
		}

		// Token: 0x060006C9 RID: 1737 RVA: 0x00017913 File Offset: 0x00015B13
		[__DynamicallyInvokable]
		public OutOfMemoryException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147024882);
		}

		// Token: 0x060006CA RID: 1738 RVA: 0x00017927 File Offset: 0x00015B27
		[__DynamicallyInvokable]
		public OutOfMemoryException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147024882);
		}

		// Token: 0x060006CB RID: 1739 RVA: 0x0001793C File Offset: 0x00015B3C
		protected OutOfMemoryException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
