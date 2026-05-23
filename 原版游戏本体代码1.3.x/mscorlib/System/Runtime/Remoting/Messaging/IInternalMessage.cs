using System;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000856 RID: 2134
	internal interface IInternalMessage
	{
		// Token: 0x17000F22 RID: 3874
		// (get) Token: 0x06005A76 RID: 23158
		// (set) Token: 0x06005A77 RID: 23159
		ServerIdentity ServerIdentityObject
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x17000F23 RID: 3875
		// (get) Token: 0x06005A78 RID: 23160
		// (set) Token: 0x06005A79 RID: 23161
		Identity IdentityObject
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x06005A7A RID: 23162
		[SecurityCritical]
		void SetURI(string uri);

		// Token: 0x06005A7B RID: 23163
		[SecurityCritical]
		void SetCallContext(LogicalCallContext callContext);

		// Token: 0x06005A7C RID: 23164
		[SecurityCritical]
		bool HasProperties();
	}
}
