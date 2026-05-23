using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007B5 RID: 1973
	[ComVisible(true)]
	public interface IRemotingTypeInfo
	{
		// Token: 0x17000E15 RID: 3605
		// (get) Token: 0x06005585 RID: 21893
		// (set) Token: 0x06005586 RID: 21894
		string TypeName
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x06005587 RID: 21895
		[SecurityCritical]
		bool CanCastTo(Type fromType, object o);
	}
}
