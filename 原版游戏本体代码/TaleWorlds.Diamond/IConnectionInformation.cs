using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000010 RID: 16
	public interface IConnectionInformation
	{
		// Token: 0x06000052 RID: 82
		string GetAddress(bool isIpv6Compatible = false);

		// Token: 0x06000053 RID: 83
		string GetCountry();
	}
}
