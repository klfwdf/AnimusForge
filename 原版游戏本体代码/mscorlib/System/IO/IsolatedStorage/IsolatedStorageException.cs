using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.IO.IsolatedStorage
{
	// Token: 0x020001B1 RID: 433
	[ComVisible(true)]
	[Serializable]
	public class IsolatedStorageException : Exception
	{
		// Token: 0x06001B4C RID: 6988 RVA: 0x0005C96E File Offset: 0x0005AB6E
		public IsolatedStorageException()
			: base(Environment.GetResourceString("IsolatedStorage_Exception"))
		{
			base.SetErrorCode(-2146233264);
		}

		// Token: 0x06001B4D RID: 6989 RVA: 0x0005C98B File Offset: 0x0005AB8B
		public IsolatedStorageException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233264);
		}

		// Token: 0x06001B4E RID: 6990 RVA: 0x0005C99F File Offset: 0x0005AB9F
		public IsolatedStorageException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233264);
		}

		// Token: 0x06001B4F RID: 6991 RVA: 0x0005C9B4 File Offset: 0x0005ABB4
		protected IsolatedStorageException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
