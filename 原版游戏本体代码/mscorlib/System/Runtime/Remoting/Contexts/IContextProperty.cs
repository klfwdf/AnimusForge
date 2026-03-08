using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x0200080D RID: 2061
	[ComVisible(true)]
	public interface IContextProperty
	{
		// Token: 0x17000EB8 RID: 3768
		// (get) Token: 0x060058C1 RID: 22721
		string Name
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x060058C2 RID: 22722
		[SecurityCritical]
		bool IsNewContextOK(Context newCtx);

		// Token: 0x060058C3 RID: 22723
		[SecurityCritical]
		void Freeze(Context newContext);
	}
}
