using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200085C RID: 2140
	[ComVisible(true)]
	public interface IMethodReturnMessage : IMethodMessage, IMessage
	{
		// Token: 0x17000F31 RID: 3889
		// (get) Token: 0x06005A91 RID: 23185
		int OutArgCount
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x06005A92 RID: 23186
		[SecurityCritical]
		string GetOutArgName(int index);

		// Token: 0x06005A93 RID: 23187
		[SecurityCritical]
		object GetOutArg(int argNum);

		// Token: 0x17000F32 RID: 3890
		// (get) Token: 0x06005A94 RID: 23188
		object[] OutArgs
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000F33 RID: 3891
		// (get) Token: 0x06005A95 RID: 23189
		Exception Exception
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000F34 RID: 3892
		// (get) Token: 0x06005A96 RID: 23190
		object ReturnValue
		{
			[SecurityCritical]
			get;
		}
	}
}
