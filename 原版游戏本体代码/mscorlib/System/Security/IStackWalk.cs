using System;
using System.Runtime.InteropServices;

namespace System.Security
{
	// Token: 0x020001D5 RID: 469
	[ComVisible(true)]
	public interface IStackWalk
	{
		// Token: 0x06001C78 RID: 7288
		void Assert();

		// Token: 0x06001C79 RID: 7289
		void Demand();

		// Token: 0x06001C7A RID: 7290
		void Deny();

		// Token: 0x06001C7B RID: 7291
		void PermitOnly();
	}
}
