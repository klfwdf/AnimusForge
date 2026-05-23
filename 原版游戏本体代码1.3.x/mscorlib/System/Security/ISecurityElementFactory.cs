using System;

namespace System.Security
{
	// Token: 0x020001BC RID: 444
	internal interface ISecurityElementFactory
	{
		// Token: 0x06001BCC RID: 7116
		SecurityElement CreateSecurityElement();

		// Token: 0x06001BCD RID: 7117
		object Copy();

		// Token: 0x06001BCE RID: 7118
		string GetTag();

		// Token: 0x06001BCF RID: 7119
		string Attribute(string attributeName);
	}
}
