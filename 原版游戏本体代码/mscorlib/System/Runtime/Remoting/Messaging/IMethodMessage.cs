using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200085A RID: 2138
	[ComVisible(true)]
	public interface IMethodMessage : IMessage
	{
		// Token: 0x17000F26 RID: 3878
		// (get) Token: 0x06005A82 RID: 23170
		string Uri
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000F27 RID: 3879
		// (get) Token: 0x06005A83 RID: 23171
		string MethodName
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000F28 RID: 3880
		// (get) Token: 0x06005A84 RID: 23172
		string TypeName
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000F29 RID: 3881
		// (get) Token: 0x06005A85 RID: 23173
		object MethodSignature
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000F2A RID: 3882
		// (get) Token: 0x06005A86 RID: 23174
		int ArgCount
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x06005A87 RID: 23175
		[SecurityCritical]
		string GetArgName(int index);

		// Token: 0x06005A88 RID: 23176
		[SecurityCritical]
		object GetArg(int argNum);

		// Token: 0x17000F2B RID: 3883
		// (get) Token: 0x06005A89 RID: 23177
		object[] Args
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000F2C RID: 3884
		// (get) Token: 0x06005A8A RID: 23178
		bool HasVarArgs
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000F2D RID: 3885
		// (get) Token: 0x06005A8B RID: 23179
		LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000F2E RID: 3886
		// (get) Token: 0x06005A8C RID: 23180
		MethodBase MethodBase
		{
			[SecurityCritical]
			get;
		}
	}
}
