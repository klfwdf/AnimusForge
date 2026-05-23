using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200085B RID: 2139
	[ComVisible(true)]
	public interface IMethodCallMessage : IMethodMessage, IMessage
	{
		// Token: 0x17000F2F RID: 3887
		// (get) Token: 0x06005A8D RID: 23181
		int InArgCount
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x06005A8E RID: 23182
		[SecurityCritical]
		string GetInArgName(int index);

		// Token: 0x06005A8F RID: 23183
		[SecurityCritical]
		object GetInArg(int argNum);

		// Token: 0x17000F30 RID: 3888
		// (get) Token: 0x06005A90 RID: 23184
		object[] InArgs
		{
			[SecurityCritical]
			get;
		}
	}
}
