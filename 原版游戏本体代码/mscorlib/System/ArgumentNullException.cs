using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x020000A7 RID: 167
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class ArgumentNullException : ArgumentException
	{
		// Token: 0x060009A3 RID: 2467 RVA: 0x0001F490 File Offset: 0x0001D690
		[__DynamicallyInvokable]
		public ArgumentNullException()
			: base(Environment.GetResourceString("ArgumentNull_Generic"))
		{
			base.SetErrorCode(-2147467261);
		}

		// Token: 0x060009A4 RID: 2468 RVA: 0x0001F4AD File Offset: 0x0001D6AD
		[__DynamicallyInvokable]
		public ArgumentNullException(string paramName)
			: base(Environment.GetResourceString("ArgumentNull_Generic"), paramName)
		{
			base.SetErrorCode(-2147467261);
		}

		// Token: 0x060009A5 RID: 2469 RVA: 0x0001F4CB File Offset: 0x0001D6CB
		[__DynamicallyInvokable]
		public ArgumentNullException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147467261);
		}

		// Token: 0x060009A6 RID: 2470 RVA: 0x0001F4E0 File Offset: 0x0001D6E0
		[__DynamicallyInvokable]
		public ArgumentNullException(string paramName, string message)
			: base(message, paramName)
		{
			base.SetErrorCode(-2147467261);
		}

		// Token: 0x060009A7 RID: 2471 RVA: 0x0001F4F5 File Offset: 0x0001D6F5
		[SecurityCritical]
		protected ArgumentNullException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
