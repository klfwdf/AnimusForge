using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000B5 RID: 181
	[ComVisible(true)]
	[Serializable]
	public class CannotUnloadAppDomainException : SystemException
	{
		// Token: 0x06000A84 RID: 2692 RVA: 0x00021815 File Offset: 0x0001FA15
		public CannotUnloadAppDomainException()
			: base(Environment.GetResourceString("Arg_CannotUnloadAppDomainException"))
		{
			base.SetErrorCode(-2146234347);
		}

		// Token: 0x06000A85 RID: 2693 RVA: 0x00021832 File Offset: 0x0001FA32
		public CannotUnloadAppDomainException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146234347);
		}

		// Token: 0x06000A86 RID: 2694 RVA: 0x00021846 File Offset: 0x0001FA46
		public CannotUnloadAppDomainException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146234347);
		}

		// Token: 0x06000A87 RID: 2695 RVA: 0x0002185B File Offset: 0x0001FA5B
		protected CannotUnloadAppDomainException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
