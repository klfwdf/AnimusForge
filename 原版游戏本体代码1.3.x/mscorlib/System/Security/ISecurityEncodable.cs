using System;
using System.Runtime.InteropServices;

namespace System.Security
{
	// Token: 0x020001D3 RID: 467
	[ComVisible(true)]
	public interface ISecurityEncodable
	{
		// Token: 0x06001C74 RID: 7284
		SecurityElement ToXml();

		// Token: 0x06001C75 RID: 7285
		void FromXml(SecurityElement e);
	}
}
