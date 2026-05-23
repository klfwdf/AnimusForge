using System;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000F5 RID: 245
	[Serializable]
	public sealed class InsufficientMemoryException : OutOfMemoryException
	{
		// Token: 0x06000F0A RID: 3850 RVA: 0x0002EDA5 File Offset: 0x0002CFA5
		public InsufficientMemoryException()
			: base(Exception.GetMessageFromNativeResources(Exception.ExceptionMessageKind.OutOfMemory))
		{
			base.SetErrorCode(-2146233027);
		}

		// Token: 0x06000F0B RID: 3851 RVA: 0x0002EDBE File Offset: 0x0002CFBE
		public InsufficientMemoryException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233027);
		}

		// Token: 0x06000F0C RID: 3852 RVA: 0x0002EDD2 File Offset: 0x0002CFD2
		public InsufficientMemoryException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233027);
		}

		// Token: 0x06000F0D RID: 3853 RVA: 0x0002EDE7 File Offset: 0x0002CFE7
		private InsufficientMemoryException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
